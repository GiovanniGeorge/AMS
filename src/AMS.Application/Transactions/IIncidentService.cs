using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;
using AMS.Application.Common;

namespace AMS.Application.Transactions;

/// <summary>What the user fills in on the "Report incident" screen.</summary>
public record IncidentInput(
    int AssetId,
    IncidentType Type,
    string? Description,
    DateTime Date);

/// <summary>Status values used by Incident (it does not follow the plain Pending/Approved/Rejected set).</summary>
public static class IncidentStatus
{
    /// <summary>Damaged / Malfunction: waiting for the approver to choose work order or disposal.</summary>
    public const string Pending = "Pending";

    /// <summary>Missing / Stolen: recorded, with a disposal request already created.</summary>
    public const string Reported = "Reported";

    /// <summary>The approver sent it to a work order.</summary>
    public const string SentToWorkOrder = "Sent to work order";

    /// <summary>The approver sent it to a disposal request.</summary>
    public const string SentToDisposal = "Sent to disposal";

    public const string Rejected = "Rejected";
}

/// <summary>
/// Incidents on assets. Damaged / Malfunction wait for the approver, who decides whether the asset
/// goes to a work order or to disposal. Missing / Stolen are not approved: a disposal request is
/// created immediately and the approver sees the asset's last stage and last custodian.
/// </summary>
public interface IIncidentService
{
    Task<Paged<Incident>> GetAllAsync(string? status = null, int page = 1);

    /// <summary>Damaged / Malfunction incidents waiting for the approver's decision.</summary>
    Task<List<Incident>> GetPendingAsync();

    /// <summary>The logged-in user's own rejected incidents, with the reason.</summary>
    Task<List<Incident>> GetMyRejectedAsync();

    /// <summary>Missing / stolen assets that have not been disposed of yet - shown to the approver.</summary>
    Task<List<Incident>> GetLossNoticesAsync();

    /// <summary>Assets an incident can be reported against: active and not already disposed of.</summary>
    Task<List<Asset>> GetReportableAssetsAsync();

    /// <summary>Records the incident. Missing / Stolen also create the disposal request.</summary>
    Task<int> CreateAsync(IncidentInput input);

    /// <summary>Approver only. Opens a corrective work order; the asset goes Under maintenance.</summary>
    Task SendToWorkOrderAsync(int incidentId);

    /// <summary>Approver only. Opens a disposal request, which still needs its own approval.</summary>
    Task SendToDisposalAsync(int incidentId);

    /// <summary>Approver only. The reason is required; the asset does not change.</summary>
    Task RejectAsync(int incidentId, string reason);
}
