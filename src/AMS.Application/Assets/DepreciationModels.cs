using AMS.Domain.Entities.Transactions;
using AMS.Application.Common;

namespace AMS.Application.Assets;

/// <summary>The length of one depreciation period.</summary>
public static class DepreciationPeriod
{
    public const string Monthly = "Monthly";
    public const string Annual = "Annual";
}

/// <summary>What one asset would get for the period - shown before anything is written.</summary>
public record DepreciationPreviewLine(
    int AssetId,
    string AssetNumber,
    string AssetName,
    decimal PurchaseCost,
    decimal Amount,
    decimal AccumulatedDepreciation,
    decimal NetBookValue,
    string? Skipped);
