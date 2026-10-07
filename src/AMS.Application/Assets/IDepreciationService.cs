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

/// <summary>
/// Straight-line depreciation posted as history, one entry per asset per period date.
/// Annual charge = (purchase cost - the category's salvage value) / the category's useful life in years.
/// A monthly period charges a twelfth of that. Nothing is ever charged below the salvage value.
/// Only the administrator can post.
/// </summary>
public interface IDepreciationService
{
    /// <summary>The entries already posted, newest first.</summary>
    Task<Paged<DepreciationEntry>> GetAllAsync(int page = 1);

    /// <summary>
    /// What would be written for <paramref name="periodEnd"/>, including the assets that
    /// would be skipped and why. Nothing is saved.
    /// </summary>
    Task<List<DepreciationPreviewLine>> PreviewAsync(string period, DateTime periodEnd);

    /// <summary>Administrator only. Writes the entries the preview showed. Returns how many were written.</summary>
    Task<int> PostAsync(string period, DateTime periodEnd);
}
