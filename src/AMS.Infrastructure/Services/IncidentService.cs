using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Application.Maintenance;
using AMS.Application.Transactions;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class IncidentService : IIncidentService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDisposalService _disposals;
    private readonly IWorkOrderService _workOrders;

    public IncidentService(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IDisposalService disposals,
        IWorkOrderService workOrders)
    {
        _db = db;
        _currentUser = currentUser;
        _disposals = disposals;
        _workOrders = workOrders;
    }

    /// <summary>Damaged and Malfunction need the approver's decision; Missing and Stolen do not.</summary>
    private static bool NeedsApproval(IncidentType type) =>
        type == IncidentType.Damaged || type == IncidentType.Malfunction;

    // ------------------------------------------------------------------
    // Reading
    // ------------------------------------------------------------------

    private IQueryable<Incident> Incidents() =>
        _db.Incidents
            .AsNoTracking()
            .Include(i => i.Asset)
            .Include(i => i.ReportedBy)
            .Include(i => i.LastCustodian)
            .Include(i => i.Approval)
            .Where(i => i.IsActive);

    public async Task<Paged<Incident>> GetAllAsync(string? status = null, int page = 1)
    {
        var query = Incidents();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(i => i.Status == status);
        }

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(i => i.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<Incident>.Size)
            .Take(Paged<Incident>.Size)
            .ToListAsync();

        return new Paged<Incident>(items, total);
    }

    public async Task<List<Incident>> GetPendingAsync()
    {
        return await Incidents()
            .Where(i => i.Status == IncidentStatus.Pending)
            .OrderBy(i => i.Id)
            .ToListAsync();
    }

    public async Task<List<Incident>> GetMyRejectedAsync()
    {
        var userId = await _currentUser.GetUserIdAsync();
        if (userId is null)
        {
            return new List<Incident>();
        }

        return await Incidents()
            .Where(i => i.Status == IncidentStatus.Rejected && i.CreatedById == userId)
            .OrderByDescending(i => i.Id)
            .ToListAsync();
    }

    public async Task<List<Incident>> GetLossNoticesAsync()
    {
        return await Incidents()
            .Where(i => (i.IncidentType == IncidentType.Missing || i.IncidentType == IncidentType.Stolen)
                        && i.Asset!.LifecycleStage != LifecycleStage.Disposed)
            .OrderByDescending(i => i.Id)
            .ToListAsync();
    }

    public async Task<List<Asset>> GetReportableAssetsAsync()
    {
        return await _db.Assets
            .AsNoTracking()
            .Include(a => a.Custodian)
            .Where(a => a.IsActive && a.LifecycleStage != LifecycleStage.Disposed)
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    // ------------------------------------------------------------------
    // Reporting an incident
    // ------------------------------------------------------------------

    public async Task<int> CreateAsync(IncidentInput input)
    {
        var asset = await _db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == input.AssetId)
                    ?? throw new InvalidOperationException("Asset not found.");

        if (!asset.IsActive || asset.LifecycleStage == LifecycleStage.Disposed)
        {
            throw new InvalidOperationException("No incident can be reported on a disposed or inactive asset.");
        }

        int incidentId;
        try
        {
            var incident = new Incident
            {
                AssetId = input.AssetId,
                IncidentType = input.Type,
                Description = input.Description,
                Date = input.Date,
                ReportedById = await _currentUser.GetUserIdAsync(),
                // Kept here because the asset moves on afterwards.
                LastStage = asset.LifecycleStage,
                LastCustodianId = asset.CustodianId,
                Status = NeedsApproval(input.Type) ? IncidentStatus.Pending : IncidentStatus.Reported
            };

            _db.Incidents.Add(incident);
            await _db.SaveChangesAsync();
            incidentId = incident.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }

        // A missing or stolen asset is not approved as an incident: it goes straight to a
        // disposal request (which the approver then decides on).
        if (!NeedsApproval(input.Type))
        {
            await _disposals.CreateAsync(
                new DisposalInput(
                    input.AssetId,
                    $"{input.Type}{(string.IsNullOrWhiteSpace(input.Description) ? "" : " - " + input.Description)}",
                    input.Date,
                    SaleProceeds: null,
                    NetBookValue: null),
                incidentId);
        }

        return incidentId;
    }

    // ------------------------------------------------------------------
    // The approver's decision
    // ------------------------------------------------------------------

    public async Task SendToWorkOrderAsync(int incidentId)
    {
        await EnsureApproverAsync();

        var incident = await LoadPendingAsync(incidentId);
        var assetId = incident.AssetId ?? throw new InvalidOperationException("The incident has no asset.");
        var date = incident.Date ?? DateTime.UtcNow;
        var problem = incident.Description;

        await DecideAsync(incidentId, IncidentStatus.SentToWorkOrder, "Sent to work order");

        await _workOrders.CreateAsync(
            new WorkOrderInput(assetId, MaintenanceType.Corrective, problem, TechnicianId: null, VendorId: null, date),
            incidentId);
    }

    public async Task SendToDisposalAsync(int incidentId)
    {
        await EnsureApproverAsync();

        var incident = await LoadPendingAsync(incidentId);
        var assetId = incident.AssetId ?? throw new InvalidOperationException("The incident has no asset.");
        var date = incident.Date ?? DateTime.UtcNow;
        var type = incident.IncidentType?.ToString() ?? "Incident";
        var description = incident.Description;

        await DecideAsync(incidentId, IncidentStatus.SentToDisposal, "Sent to disposal");

        await _disposals.CreateAsync(
            new DisposalInput(
                assetId,
                $"{type}{(string.IsNullOrWhiteSpace(description) ? "" : " - " + description)}",
                date,
                SaleProceeds: null,
                NetBookValue: null),
            incidentId);
    }

    public async Task RejectAsync(int incidentId, string reason)
    {
        await EnsureApproverAsync();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A rejection reason is required.");
        }

        await LoadPendingAsync(incidentId);
        await DecideAsync(incidentId, IncidentStatus.Rejected, reason.Trim(), RequestStatus.Rejected);
    }

    /// <summary>Writes the approval row and the new status on the incident.</summary>
    private async Task DecideAsync(int incidentId, string status, string? comments, string? decision = null)
    {
        try
        {
            var incident = await _db.Incidents.FirstOrDefaultAsync(i => i.Id == incidentId && i.IsActive)
                           ?? throw new InvalidOperationException("Incident not found.");

            incident.Approval = new Approval
            {
                ApproverId = await _currentUser.GetUserIdAsync(),
                Decision = decision ?? RequestStatus.Approved,
                Status = decision ?? RequestStatus.Approved,
                Comments = comments,
                DecisionDate = DateTime.UtcNow
            };
            incident.Status = status;

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
            throw new InvalidOperationException("Only the administrator can decide on incidents.");
        }
    }

    private async Task<Incident> LoadPendingAsync(int incidentId)
    {
        var incident = await _db.Incidents
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == incidentId && i.IsActive)
            ?? throw new InvalidOperationException("Incident not found.");

        if (incident.Status != IncidentStatus.Pending)
        {
            throw new InvalidOperationException("This incident has already been decided.");
        }

        return incident;
    }
}
