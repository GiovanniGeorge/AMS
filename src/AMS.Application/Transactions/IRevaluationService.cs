using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Application.Common;

namespace AMS.Application.Transactions;

/// <summary>What the user fills in on the "Revaluation" screen.</summary>
public record RevaluationInput(
    int AssetId,
    decimal NewValue,
    DateTime Date,
    string? Reason);

/// <summary>
/// Re-stating an asset's value. There is no approval and the asset's purchase cost is not touched:
/// this is a history of values only.
/// </summary>
public interface IRevaluationService
{
    Task<Paged<RevaluationEvent>> GetAllAsync(int page = 1);

    /// <summary>Assets that can be revalued: active and not disposed of.</summary>
    Task<List<Asset>> GetRevaluableAssetsAsync();

    /// <summary>The value the next revaluation starts from: the last new value, or the purchase cost.</summary>
    Task<decimal?> GetCurrentValueAsync(int assetId);

    Task<int> CreateAsync(RevaluationInput input);
}
