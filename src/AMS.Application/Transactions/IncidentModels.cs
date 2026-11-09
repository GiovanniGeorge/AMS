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
