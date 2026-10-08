using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;

namespace AMS.Application.Assets;

/// <summary>Filters for the Assets Register. Every filter is optional.</summary>
/// <param name="PageSize">0 or less means "no paging" - used by the CSV export.</param>
public record AssetFilter(
    string? Search = null,
    LifecycleStage? Stage = null,
    int? CategoryId = null,
    int? LocationId = null,
    bool IncludeInactive = false,
    int Page = 1,
    int PageSize = 50,
    int? StatusId = null);

/// <summary>One page of the register plus the total number of assets that match the filter.</summary>
public record AssetPage(List<Asset> Items, int Total);

/// <summary>One line in an asset's lifecycle history.</summary>
public record AssetHistoryEntry(DateTime Date, string Type, string Description, int? UserId);

/// <summary>
/// The asset is not simple CRUD: it has automatic numbering, a lifecycle stage,
/// fields that only transactions may change, and a history built from many tables.
/// Deactivate / Activate still use ICrudService&lt;Asset&gt;.
/// </summary>
public interface IAssetService
{
    Task<AssetPage> SearchAsync(AssetFilter filter);

    /// <summary>One asset with all its lookups (category, status, location, custodian, ...).</summary>
    Task<Asset?> GetDetailsAsync(int id);

    /// <summary>Active assets whose ParentAssetId is <paramref name="parentId"/>.</summary>
    Task<List<Asset>> GetComponentsAsync(int parentId);

    /// <summary>Registration plus every active transaction that touched the asset, oldest first.</summary>
    Task<List<AssetHistoryEntry>> GetHistoryAsync(int assetId);

    /// <summary>The number the next asset of this category would get, or null when the category has no prefix.</summary>
    Task<string?> PreviewNextNumberAsync(int categoryId);

    /// <summary>
    /// Registers a new asset: generates the number from the category prefix (when it has one)
    /// and sets the stage (Assigned when a custodian is given, otherwise InStore). Returns the new Id.
    /// </summary>
    Task<int> RegisterAsync(Asset asset);

    /// <summary>
    /// Saves the descriptive fields only. AssetNumber, LifecycleStage, Location,
    /// Department and Custodian are never changed here - only by transactions.
    /// </summary>
    Task UpdateDetailsAsync(Asset asset);
}
