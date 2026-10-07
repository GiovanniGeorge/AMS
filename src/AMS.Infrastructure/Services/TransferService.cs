using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Application.Transactions;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class TransferService : ITransferService
{
    /// <summary>Approved and dispatched, waiting for the receiving side to confirm.</summary>
    public const string InTransit = "In transit";

    /// <summary>Receipt confirmed - the transfer is finished.</summary>
    public const string Received = "Received";

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TransferService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ------------------------------------------------------------------
    // Reading
    // ------------------------------------------------------------------

    private IQueryable<Transfer> Transfers() =>
        _db.Transfers
            .AsNoTracking()
            .Include(t => t.FromCustodian)
            .Include(t => t.FromDepartment)
            .Include(t => t.FromLocation)
            .Include(t => t.ToCustodian)
            .Include(t => t.ToDepartment)
            .Include(t => t.ToLocation)
            .Include(t => t.Approval)
            .Include(t => t.Lines.Where(l => l.IsActive))
                .ThenInclude(l => l.Asset)
            .Where(t => t.IsActive);

    public async Task<Paged<Transfer>> GetAllAsync(string? status = null, int page = 1)
    {
        var query = Transfers();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.Status == status);
        }

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(t => t.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<Transfer>.Size)
            .Take(Paged<Transfer>.Size)
            .ToListAsync();

        return new Paged<Transfer>(items, total);
    }

    public async Task<List<Transfer>> GetPendingAsync()
    {
        return await Transfers()
            .Where(t => t.Status == RequestStatus.Pending)
            .OrderBy(t => t.Id)
            .ToListAsync();
    }

    public async Task<List<Transfer>> GetMyRejectedAsync()
    {
        var userId = await _currentUser.GetUserIdAsync();
        if (userId is null)
        {
            return new List<Transfer>();
        }

        return await Transfers()
            .Where(t => t.Status == RequestStatus.Rejected && t.CreatedById == userId)
            .OrderByDescending(t => t.Id)
            .ToListAsync();
    }

    public async Task<List<Asset>> GetTransferableAssetsAsync()
    {
        // An asset inside any unfinished request - transfer, issuance or disposal - is busy.
        var busy = PendingRequests.AssetIds(_db);

        return await _db.Assets
            .AsNoTracking()
            .Include(a => a.Custodian)
            .Include(a => a.Location)
            .Where(a => a.IsActive
                        && (a.LifecycleStage == LifecycleStage.InStore || a.LifecycleStage == LifecycleStage.Assigned)
                        && !busy.Contains(a.Id))
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    // ------------------------------------------------------------------
    // Creating a request
    // ------------------------------------------------------------------

    public async Task<int> CreateAsync(TransferInput input)
    {
        if (input.ToCustodianId is null && input.ToDepartmentId is null && input.ToLocationId is null)
        {
            throw new InvalidOperationException("Give at least one destination: custodian, department or location.");
        }

        try
        {
            var asset = await _db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == input.AssetId)
                        ?? throw new InvalidOperationException("Asset not found.");

            if (!asset.IsActive
                || (asset.LifecycleStage != LifecycleStage.InStore && asset.LifecycleStage != LifecycleStage.Assigned))
            {
                throw new InvalidOperationException("Only an active asset that is In store or Assigned can be transferred.");
            }

            var busy = await _db.TransferLines.AnyAsync(l => l.IsActive
                                                             && l.AssetId == input.AssetId
                                                             && l.Transfer!.IsActive
                                                             && (l.Transfer.Status == RequestStatus.Pending
                                                                 || l.Transfer.Status == InTransit));
            if (busy)
            {
                throw new InvalidOperationException("This asset already has a transfer in progress.");
            }

            await PendingRequests.EnsureFreeAsync(_db, input.AssetId);

            var transfer = new Transfer
            {
                Date = input.Date,
                FromCustodianId = asset.CustodianId,
                FromDepartmentId = asset.DepartmentId,
                FromLocationId = asset.LocationId,
                ToCustodianId = input.ToCustodianId,
                ToDepartmentId = input.ToDepartmentId,
                ToLocationId = input.ToLocationId,
                Status = RequestStatus.Pending
            };
            transfer.Lines.Add(new TransferLine { AssetId = input.AssetId });

            _db.Transfers.Add(transfer);
            await _db.SaveChangesAsync();
            return transfer.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    // ------------------------------------------------------------------
    // Approving / rejecting / receiving
    // ------------------------------------------------------------------

    public async Task ApproveAsync(int transferId)
    {
        await EnsureApproverAsync();

        try
        {
            var transfer = await LoadPendingAsync(transferId);
            var assetIds = transfer.Lines.Where(l => l.IsActive).Select(l => l.AssetId).ToList();
            var assets = await _db.Assets.Where(a => assetIds.Contains(a.Id)).ToListAsync();

            foreach (var asset in assets)
            {
                if (!asset.IsActive
                    || (asset.LifecycleStage != LifecycleStage.InStore && asset.LifecycleStage != LifecycleStage.Assigned))
                {
                    throw new InvalidOperationException($"Asset {asset.AssetNumber} can no longer be transferred.");
                }

                asset.LifecycleStage = LifecycleStage.InTransit;
            }

            transfer.Approval = await NewApprovalAsync(RequestStatus.Approved, comments: null);
            transfer.Status = InTransit;
            transfer.DispatchDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task RejectAsync(int transferId, string reason)
    {
        await EnsureApproverAsync();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A rejection reason is required.");
        }

        try
        {
            var transfer = await LoadPendingAsync(transferId);

            transfer.Approval = await NewApprovalAsync(RequestStatus.Rejected, reason.Trim());
            transfer.Status = RequestStatus.Rejected;

            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task ConfirmReceiptAsync(int transferId)
    {
        try
        {
            var transfer = await _db.Transfers
                .Include(t => t.Lines)
                .FirstOrDefaultAsync(t => t.Id == transferId && t.IsActive)
                ?? throw new InvalidOperationException("Transfer not found.");

            if (transfer.Status != InTransit)
            {
                throw new InvalidOperationException("Only a transfer that is In transit can be confirmed.");
            }

            var assetIds = transfer.Lines.Where(l => l.IsActive).Select(l => l.AssetId).ToList();
            var assets = await _db.Assets.Where(a => assetIds.Contains(a.Id)).ToListAsync();

            var now = DateTime.UtcNow;
            var arrivalDate = transfer.Date ?? now;

            foreach (var asset in assets)
            {
                // Close the custody the asset left.
                var openCustody = await _db.AssetAssignments
                    .Where(a => a.IsActive && a.AssetId == asset.Id && a.ReturnDate == null
                                && a.Status == RequestStatus.Approved)
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefaultAsync();

                if (openCustody is not null)
                {
                    openCustody.ReturnDate = arrivalDate;
                }

                asset.CustodianId = transfer.ToCustodianId;
                asset.DepartmentId = transfer.ToDepartmentId;
                asset.LocationId = transfer.ToLocationId ?? asset.LocationId;
                asset.LifecycleStage = transfer.ToCustodianId is null
                    ? LifecycleStage.InStore
                    : LifecycleStage.Assigned;

                // A transfer to a custodian opens a new custody period (no Issue behind it).
                if (transfer.ToCustodianId is int custodianId)
                {
                    _db.AssetAssignments.Add(new AssetAssignment
                    {
                        AssetId = asset.Id,
                        CustodianId = custodianId,
                        DepartmentId = transfer.ToDepartmentId,
                        LocationId = transfer.ToLocationId,
                        AssignmentDate = arrivalDate,
                        Status = RequestStatus.Approved,
                        ApprovalId = transfer.ApprovalId,
                        Notes = $"Opened by transfer #{transfer.Id}"
                    });
                }
            }

            transfer.Status = Received;
            transfer.ReceiveConfirmationDate = now;

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

    private async Task<Transfer> LoadPendingAsync(int transferId)
    {
        var transfer = await _db.Transfers
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.IsActive)
            ?? throw new InvalidOperationException("Transfer not found.");

        if (transfer.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("This transfer has already been decided.");
        }

        return transfer;
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
