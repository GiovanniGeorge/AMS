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

/// <summary>
/// A physical count of the assets that match a scope (location / department / category).
/// The approved entity list has no line table, so the campaign keeps the expected assets
/// as a snapshot of asset numbers, and the progress and findings as plain text.
/// </summary>
public interface IVerificationService
{
    Task<List<VerificationCampaign>> GetAllAsync();

    Task<int> CreateAsync(VerificationInput input);

    Task UpdateAsync(int id, VerificationInput input);

    /// <summary>How many active assets match the scope right now.</summary>
    Task<int> CountScopeAsync(int? locationId, int? departmentId, int? categoryId);

    /// <summary>
    /// Writes the asset numbers that match the scope into ExpectedSnapshot and starts the campaign.
    /// Re-running it replaces the snapshot.
    /// </summary>
    Task TakeSnapshotAsync(int id);

    /// <summary>Saves what was found: progress as a percentage and the findings as text.</summary>
    Task SaveResultsAsync(int id, decimal? progress, string? results, bool close);
}
