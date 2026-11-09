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
