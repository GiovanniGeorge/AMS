using AMS.Domain.Entities.Main;

namespace AMS.Application.Assets;

/// <summary>What the user fills in on the "Verification campaign" screen.</summary>
public record VerificationInput(
    string Name,
    int? LocationId,
    int? DepartmentId,
    int? CategoryId,
    DateTime? StartDate,
    DateTime? EndDate);

/// <summary>Status values used by VerificationCampaign.</summary>
public static class VerificationStatus
{
    public const string Draft = "Draft";
    public const string InProgress = "In progress";
    public const string Closed = "Closed";
}
