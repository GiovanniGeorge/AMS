using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Application.Transactions;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

/// <summary>
/// Asset issuance: one request creates an Issue (asset leaves the store) and an
/// AssetAssignment (the custody), both Pending. The approver approves or rejects the assignment.
/// </summary>
public class AssetIssuanceService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ICurrentUserService _currentUser;

    public AssetIssuanceService(IDbContextFactory<ApplicationDbContext> dbFactory, ICurrentUserService currentUser)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
    }

    // ------------------------------------------------------------------
    // Reading
    // ------------------------------------------------------------------

    private IQueryable<AssetAssignment> Issuances(ApplicationDbContext db) =>
        db.AssetAssignments
            .AsNoTracking()
            .Include(a => a.Asset)
            .Include(a => a.Custodian)
            .Include(a => a.Department)
            .Include(a => a.Location)
            .Include(a => a.Issue!).ThenInclude(i => i.Store)
            .Include(a => a.Approval)
            .Where(a => a.IsActive && a.IssueId != null);

    /// <summary>All issuance requests, newest first. <paramref name="status"/> null = all.</summary>
    public async Task<Paged<AssetAssignment>> GetAllAsync(string? status = null, int page = 1)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = Issuances(db);

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

    /// <summary>Requests waiting for the approver.</summary>
    public async Task<List<AssetAssignment>> GetPendingAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await Issuances(db)
            .Where(a => a.Status == RequestStatus.Pending)
            .OrderBy(a => a.Id)
            .ToListAsync();
    }

    /// <summary>The logged-in user's own rejected requests, with the rejection reason.</summary>
    public async Task<List<AssetAssignment>> GetMyRejectedAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var userId = await _currentUser.GetUserIdAsync();
        if (userId is null)
        {
            return new List<AssetAssignment>();
        }

        return await Issuances(db)
            .Where(a => a.Status == RequestStatus.Rejected && a.CreatedById == userId)
            .OrderByDescending(a => a.Id)
            .ToListAsync();
    }

    /// <summary>Assets that can be issued: active, In store, and without a pending request.</summary>
    public async Task<List<Asset>> GetIssuableAssetsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Not just a pending issuance: an asset inside a transfer or a disposal is busy too.
        var busy = PendingRequests.AssetIds(db);

        return await db.Assets
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

    /// <summary>Creates the Issue + AssetAssignment pair. Returns the assignment Id.</summary>
    public async Task<int> CreateAsync(IssuanceRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var asset = await db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AssetId)
                    ?? throw new InvalidOperationException("Asset not found.");

        if (!asset.IsActive || asset.LifecycleStage != LifecycleStage.InStore)
        {
            throw new InvalidOperationException("Only an active asset that is In store can be issued.");
        }

        var hasPending = await db.AssetAssignments
            .AnyAsync(a => a.IsActive && a.AssetId == request.AssetId && a.Status == RequestStatus.Pending);
        if (hasPending)
        {
            throw new InvalidOperationException("This asset already has a request waiting for approval.");
        }

        await PendingRequests.EnsureFreeAsync(db, request.AssetId);

        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.IsActive)
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

        db.AssetAssignments.Add(assignment); // the Issue is saved with it
        await db.SaveChangesAsync();
        return assignment.Id;
    }

    /// <summary>
    /// Administrator only: creates the issuance and approves it in the same step, so the asset
    /// moves to the employee at once. Used by the Assign button on the employee and asset screens -
    /// the administrator is the approver, so asking him to approve his own request adds nothing.
    /// </summary>
    public async Task<int> CreateApprovedAsync(IssuanceRequest request)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Only the administrator may move custody. A direct assignment skips the waiting
        // step, not the permission check.
        await EnsureApproverAsync();

        // Tracked: this asset is updated below in the same SaveChanges.
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId)
                    ?? throw new InvalidOperationException("Asset not found.");

        if (!asset.IsActive || asset.LifecycleStage != LifecycleStage.InStore)
        {
            throw new InvalidOperationException("Only an active asset that is In store can be issued.");
        }

        await PendingRequests.EnsureFreeAsync(db, request.AssetId);

        var employee = await db.Employees.AsNoTracking()
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

        db.AssetAssignments.Add(assignment);
        await db.SaveChangesAsync();
        return assignment.Id;
    }

    // ------------------------------------------------------------------
    // Approving / rejecting
    // ------------------------------------------------------------------

    /// <summary>Approver only. The asset takes the custodian, department and location and becomes Assigned.</summary>
    public async Task ApproveAsync(int assignmentId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        await EnsureApproverAsync();

        var assignment = await LoadPendingAsync(db, assignmentId);
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

        await db.SaveChangesAsync();
    }

    /// <summary>Approver only. The reason is required; the asset does not change.</summary>
    public async Task RejectAsync(int assignmentId, string reason)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        await EnsureApproverAsync();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A rejection reason is required.");
        }

        var assignment = await LoadPendingAsync(db, assignmentId);

        assignment.Approval = await NewApprovalAsync(RequestStatus.Rejected, reason.Trim());
        assignment.Status = RequestStatus.Rejected;
        assignment.Issue!.Status = RequestStatus.Rejected;

        await db.SaveChangesAsync();
    }

    private async Task EnsureApproverAsync()
    {
        if (!await _currentUser.IsInRoleAsync(AppRoles.Admin))
        {
            throw new InvalidOperationException("Only the administrator can approve or reject requests.");
        }
    }

    private async Task<AssetAssignment> LoadPendingAsync(ApplicationDbContext db, int assignmentId)
    {
        var assignment = await db.AssetAssignments
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
