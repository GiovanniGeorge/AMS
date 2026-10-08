using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Application.Transactions;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class AssetIssuanceService : IAssetIssuanceService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AssetIssuanceService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ------------------------------------------------------------------
    // Reading
    // ------------------------------------------------------------------

    private IQueryable<AssetAssignment> Issuances() =>
        _db.AssetAssignments
            .AsNoTracking()
            .Include(a => a.Asset)
            .Include(a => a.Custodian)
            .Include(a => a.Department)
            .Include(a => a.Location)
            .Include(a => a.Issue!).ThenInclude(i => i.Store)
            .Include(a => a.Approval)
            .Where(a => a.IsActive && a.IssueId != null);

    public async Task<Paged<AssetAssignment>> GetAllAsync(string? status = null, int page = 1)
    {
        var query = Issuances();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(a => a.Status == status);
        }

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(a => a.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<AssetAssignment>.Size)
            .Take(Paged<AssetAssignment>.Size)
            .ToListAsync();

        return new Paged<AssetAssignment>(items, total);
    }

    public async Task<List<AssetAssignment>> GetPendingAsync()
    {
        return await Issuances()
            .Where(a => a.Status == RequestStatus.Pending)
            .OrderBy(a => a.Id)
            .ToListAsync();
    }

    public async Task<List<AssetAssignment>> GetMyRejectedAsync()
    {
        var userId = await _currentUser.GetUserIdAsync();
        if (userId is null)
        {
            return new List<AssetAssignment>();
        }

        return await Issuances()
            .Where(a => a.Status == RequestStatus.Rejected && a.CreatedById == userId)
            .OrderByDescending(a => a.Id)
            .ToListAsync();
    }

    public async Task<List<Asset>> GetIssuableAssetsAsync()
    {
        // Not just a pending issuance: an asset inside a transfer or a disposal is busy too.
        var busy = PendingRequests.AssetIds(_db);

        return await _db.Assets
            .AsNoTracking()
            .Where(a => a.IsActive
                        && a.LifecycleStage == LifecycleStage.InStore
                        && !busy.Contains(a.Id))
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    // ------------------------------------------------------------------
    // Creating a request
    // ------------------------------------------------------------------

    public async Task<int> CreateAsync(IssuanceRequest request)
    {
        var asset = await _db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AssetId)
                    ?? throw new InvalidOperationException("Asset not found.");

        if (!asset.IsActive || asset.LifecycleStage != LifecycleStage.InStore)
        {
            throw new InvalidOperationException("Only an active asset that is In store can be issued.");
        }

        var hasPending = await _db.AssetAssignments
            .AnyAsync(a => a.IsActive && a.AssetId == request.AssetId && a.Status == RequestStatus.Pending);
        if (hasPending)
        {
            throw new InvalidOperationException("This asset already has a request waiting for approval.");
        }

        await PendingRequests.EnsureFreeAsync(_db, request.AssetId);

        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.IsActive)
                       ?? throw new InvalidOperationException("Employee not found.");

        var issue = new Issue
        {
            Date = request.Date,
            AssetId = request.AssetId,
            EmployeeId = request.EmployeeId,
            StoreId = request.StoreId,
            Status = RequestStatus.Pending
        };

        var assignment = new AssetAssignment
        {
            AssetId = request.AssetId,
            CustodianId = request.EmployeeId,
            DepartmentId = request.DepartmentId ?? employee.DepartmentId,
            LocationId = request.LocationId,
            AssignmentDate = request.Date,
            Status = RequestStatus.Pending,
            Notes = request.Notes,
            Issue = issue
        };

        try
        {
            _db.AssetAssignments.Add(assignment); // the Issue is saved with it
            await _db.SaveChangesAsync();
            return assignment.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task<int> CreateApprovedAsync(IssuanceRequest request)
    {
        // Only the administrator may move custody. A direct assignment skips the waiting
        // step, not the permission check.
        await EnsureApproverAsync();

        try
        {
            // Tracked: this asset is updated below in the same SaveChanges.
            var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId)
                        ?? throw new InvalidOperationException("Asset not found.");

            if (!asset.IsActive || asset.LifecycleStage != LifecycleStage.InStore)
            {
                throw new InvalidOperationException("Only an active asset that is In store can be issued.");
            }

            await PendingRequests.EnsureFreeAsync(_db, request.AssetId);

            var employee = await _db.Employees.AsNoTracking()
                               .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.IsActive)
                           ?? throw new InvalidOperationException("Employee not found.");

            var issue = new Issue
            {
                Date = request.Date,
                AssetId = request.AssetId,
                EmployeeId = request.EmployeeId,
                StoreId = request.StoreId,
                Status = RequestStatus.Approved
            };

            var assignment = new AssetAssignment
            {
                AssetId = request.AssetId,
                CustodianId = request.EmployeeId,
                DepartmentId = request.DepartmentId ?? employee.DepartmentId,
                LocationId = request.LocationId,
                AssignmentDate = request.Date,
                Status = RequestStatus.Approved,
                Notes = request.Notes,
                Issue = issue,
                Approval = await NewApprovalAsync(RequestStatus.Approved,
                    "Assigned directly by the administrator.")
            };

            // Same effect as an approval, so the asset takes the custody immediately.
            asset.CustodianId = assignment.CustodianId;
            asset.DepartmentId = assignment.DepartmentId;
            asset.LocationId = assignment.LocationId;
            asset.LifecycleStage = LifecycleStage.Assigned;

            _db.AssetAssignments.Add(assignment);
            await _db.SaveChangesAsync();
            return assignment.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    // ------------------------------------------------------------------
    // Approving / rejecting
    // ------------------------------------------------------------------

    public async Task ApproveAsync(int assignmentId)
    {
        await EnsureApproverAsync();

        try
        {
            var assignment = await LoadPendingAsync(assignmentId);
            var asset = assignment.Asset!;

            // Checked again: the asset may have changed since the request was made.
            if (!asset.IsActive || asset.LifecycleStage != LifecycleStage.InStore)
            {
                throw new InvalidOperationException("The asset is no longer In store, so it cannot be issued.");
            }

            assignment.Approval = await NewApprovalAsync(RequestStatus.Approved, comments: null);
            assignment.Status = RequestStatus.Approved;
            assignment.Issue!.Status = RequestStatus.Approved;

            // The approved custody becomes the asset's current custody.
            asset.CustodianId = assignment.CustodianId;
            asset.DepartmentId = assignment.DepartmentId;
            asset.LocationId = assignment.LocationId;
            asset.LifecycleStage = LifecycleStage.Assigned;

            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task RejectAsync(int assignmentId, string reason)
    {
        await EnsureApproverAsync();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A rejection reason is required.");
        }

        try
        {
            var assignment = await LoadPendingAsync(assignmentId);

            assignment.Approval = await NewApprovalAsync(RequestStatus.Rejected, reason.Trim());
            assignment.Status = RequestStatus.Rejected;
            assignment.Issue!.Status = RequestStatus.Rejected;

            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    private async Task EnsureApproverAsync()
    {
        if (!await _currentUser.IsInRoleAsync(AppRoles.Admin))
        {
            throw new InvalidOperationException("Only the administrator can approve or reject requests.");
        }
    }

    private async Task<AssetAssignment> LoadPendingAsync(int assignmentId)
    {
        var assignment = await _db.AssetAssignments
            .Include(a => a.Asset)
            .Include(a => a.Issue)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.IsActive)
            ?? throw new InvalidOperationException("Request not found.");

        if (assignment.Status != RequestStatus.Pending || assignment.Issue is null)
        {
            throw new InvalidOperationException("This request has already been decided.");
        }

        return assignment;
    }

    private async Task<Approval> NewApprovalAsync(string decision, string? comments)
    {
        return new Approval
        {
            ApproverId = await _currentUser.GetUserIdAsync(),
            Decision = decision,
            Status = decision,
            Comments = comments,
            DecisionDate = DateTime.UtcNow
        };
    }
}
