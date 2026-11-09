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
/// Taking an asset out of service for good. The approver approves it; the asset then becomes Disposed.
/// The net book value is suggested by straight-line depreciation
/// (purchase cost, the category's useful life in years and salvage value) and can be edited.
/// Gain / loss = sale proceeds - net book value.
/// </summary>
public class DisposalService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ICurrentUserService _currentUser;

    public DisposalService(IDbContextFactory<ApplicationDbContext> dbFactory, ICurrentUserService currentUser)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
    }

    // ------------------------------------------------------------------
    // Reading
    // ------------------------------------------------------------------

    private IQueryable<DisposalRequest> Requests(ApplicationDbContext db) =>
        db.DisposalRequests
            .AsNoTracking()
            .Include(d => d.RequestedBy)
            .Include(d => d.Approval)
            .Include(d => d.Incident)
            .Include(d => d.Lines.Where(l => l.IsActive))
                .ThenInclude(l => l.Asset)
            .Where(d => d.IsActive);

    public async Task<Paged<DisposalRequest>> GetAllAsync(string? status = null, int page = 1)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = Requests(db);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(d => d.Status == status);
        }

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(d => d.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<DisposalRequest>.Size)
            .Take(Paged<DisposalRequest>.Size)
            .ToListAsync();

        return new Paged<DisposalRequest>(items, total);
    }

    /// <summary>Requests waiting for the approver.</summary>
    public async Task<List<DisposalRequest>> GetPendingAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await Requests(db)
            .Where(d => d.Status == RequestStatus.Pending)
            .OrderBy(d => d.Id)
            .ToListAsync();
    }

    /// <summary>The logged-in user's own rejected requests, with the reason.</summary>
    public async Task<List<DisposalRequest>> GetMyRejectedAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var userId = await _currentUser.GetUserIdAsync();
        if (userId is null)
        {
            return new List<DisposalRequest>();
        }

        return await Requests(db)
            .Where(d => d.Status == RequestStatus.Rejected && d.CreatedById == userId)
            .OrderByDescending(d => d.Id)
            .ToListAsync();
    }

    /// <summary>Assets that can be disposed of: active, not already disposed, with no pending request.</summary>
    public async Task<List<Asset>> GetDisposableAssetsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // An asset inside any unfinished request - disposal, issuance or transfer - is busy.
        var busy = PendingRequests.AssetIds(db);

        return await db.Assets
            .AsNoTracking()
            .Where(a => a.IsActive
                        && a.LifecycleStage != LifecycleStage.Disposed
                        && !busy.Contains(a.Id))
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    // ------------------------------------------------------------------
    // Net book value
    // ------------------------------------------------------------------

    /// <summary>
    /// The suggested net book value: the latest stored depreciation entry if there is one,
    /// otherwise straight line from the purchase cost. Null when the asset has no purchase cost.
    /// </summary>
    public async Task<decimal?> SuggestNetBookValueAsync(int assetId, DateTime asOf)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // A stored depreciation history wins over a calculation.
        var latest = await db.DepreciationEntries
            .AsNoTracking()
            .Where(e => e.IsActive && e.AssetId == assetId && e.PeriodDate <= asOf)
            .OrderByDescending(e => e.PeriodDate)
            .Select(e => (decimal?)e.NetBookValue)
            .FirstOrDefaultAsync();

        if (latest is not null)
        {
            return decimal.Round(latest.Value, 2);
        }

        var asset = await db.Assets
            .AsNoTracking()
            .Where(a => a.Id == assetId)
            .Select(a => new
            {
                a.PurchaseCost,
                a.PurchaseDate,
                UsefulLife = a.Category!.UsefulLife,
                Salvage = a.Category.SalvageValue
            })
            .FirstOrDefaultAsync();

        if (asset?.PurchaseCost is not decimal cost)
        {
            return null;
        }

        var salvage = asset.Salvage ?? 0m;
        if (salvage > cost)
        {
            salvage = cost;
        }

        // Useful life is in YEARS. Without it (or without a purchase date) nothing is depreciated.
        if (asset.UsefulLife is not decimal years || years <= 0 || asset.PurchaseDate is not DateTime bought)
        {
            return decimal.Round(cost, 2);
        }

        var elapsedYears = (decimal)((asOf - bought).TotalDays / 365.25);
        if (elapsedYears < 0)
        {
            elapsedYears = 0;
        }

        var depreciable = cost - salvage;
        var accumulated = depreciable / years * elapsedYears;
        if (accumulated > depreciable)
        {
            accumulated = depreciable;
        }

        return decimal.Round(cost - accumulated, 2);
    }

    // ------------------------------------------------------------------
    // Creating a request
    // ------------------------------------------------------------------

    /// <summary>
    /// <paramref name="incidentId"/> is passed by IncidentService: a missing / stolen asset,
    /// or an incident the approver sent to disposal, creates the same request with the incident linked.
    /// </summary>
    public async Task<int> CreateAsync(DisposalInput input, int? incidentId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var asset = await db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == input.AssetId)
                    ?? throw new InvalidOperationException("Asset not found.");

        if (!asset.IsActive || asset.LifecycleStage == LifecycleStage.Disposed)
        {
            throw new InvalidOperationException("This asset has already been disposed of.");
        }

        var hasPending = await db.DisposalRequestLines
            .AnyAsync(l => l.IsActive && l.AssetId == input.AssetId
                           && l.DisposalRequest!.IsActive
                           && l.DisposalRequest.Status == RequestStatus.Pending);
        if (hasPending)
        {
            throw new InvalidOperationException("This asset already has a disposal request waiting for approval.");
        }

        // An incident creates the disposal request itself, so this guard is skipped there.
        if (incidentId is null)
        {
            await PendingRequests.EnsureFreeAsync(db, input.AssetId);
        }

        var netBookValue = input.NetBookValue
                           ?? await SuggestNetBookValueAsync(input.AssetId, input.RequestDate);

        var request = new DisposalRequest
        {
            DisposalReason = input.DisposalReason,
            RequestDate = input.RequestDate,
            RequestedById = await _currentUser.GetUserIdAsync(),
            SaleProceeds = input.SaleProceeds,
            NetBookValue = netBookValue,
            GainLoss = GainLoss(input.SaleProceeds, netBookValue),
            Status = RequestStatus.Pending,
            IncidentId = incidentId
        };
        request.Lines.Add(new DisposalRequestLine { AssetId = input.AssetId });

        db.DisposalRequests.Add(request);
        await db.SaveChangesAsync();
        return request.Id;
    }

    private static decimal? GainLoss(decimal? saleProceeds, decimal? netBookValue)
    {
        if (saleProceeds is null && netBookValue is null)
        {
            return null;
        }

        return (saleProceeds ?? 0m) - (netBookValue ?? 0m);
    }

    // ------------------------------------------------------------------
    // Approving / rejecting
    // ------------------------------------------------------------------

    /// <summary>Approver only. The asset becomes Disposed and its custody is closed.</summary>
    public async Task ApproveAsync(int disposalRequestId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        await EnsureApproverAsync();

        var request = await LoadPendingAsync(db, disposalRequestId);
        var assetIds = request.Lines.Where(l => l.IsActive).Select(l => l.AssetId).ToList();
        var assets = await db.Assets.Where(a => assetIds.Contains(a.Id)).ToListAsync();

        var disposalDate = request.RequestDate ?? DateTime.UtcNow;

        foreach (var asset in assets)
        {
            if (asset.LifecycleStage == LifecycleStage.Disposed)
            {
                throw new InvalidOperationException($"Asset {asset.AssetNumber} has already been disposed of.");
            }

            var openCustody = await db.AssetAssignments
                .Where(a => a.IsActive && a.AssetId == asset.Id && a.ReturnDate == null
                            && a.Status == RequestStatus.Approved)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();

            if (openCustody is not null)
            {
                openCustody.ReturnDate = disposalDate;
            }

            asset.CustodianId = null;
            asset.LifecycleStage = LifecycleStage.Disposed;
        }

        request.GainLoss = GainLoss(request.SaleProceeds, request.NetBookValue);
        request.Approval = await NewApprovalAsync(RequestStatus.Approved, comments: null);
        request.Status = RequestStatus.Approved;

        await db.SaveChangesAsync();
    }

    /// <summary>Approver only. The reason is required; the asset does not change.</summary>
    public async Task RejectAsync(int disposalRequestId, string reason)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        await EnsureApproverAsync();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A rejection reason is required.");
        }

        var request = await LoadPendingAsync(db, disposalRequestId);

        request.Approval = await NewApprovalAsync(RequestStatus.Rejected, reason.Trim());
        request.Status = RequestStatus.Rejected;

        await db.SaveChangesAsync();
    }

    private async Task EnsureApproverAsync()
    {
        if (!await _currentUser.IsInRoleAsync(AppRoles.Admin))
        {
            throw new InvalidOperationException("Only the administrator can approve or reject requests.");
        }
    }

    private async Task<DisposalRequest> LoadPendingAsync(ApplicationDbContext db, int disposalRequestId)
    {
        var request = await db.DisposalRequests
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == disposalRequestId && d.IsActive)
            ?? throw new InvalidOperationException("Disposal request not found.");

        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("This request has already been decided.");
        }

        return request;
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
