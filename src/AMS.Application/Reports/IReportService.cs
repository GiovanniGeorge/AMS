using AMS.Domain.Enums;

namespace AMS.Application.Reports;

/// <summary>
/// One asset that entered the books inside the reported period.
/// An asset with no purchase date is still reported, dated by its registration instead,
/// so nothing bought is ever silently missing from the figures.
/// </summary>
public record PurchasedAsset(
    int AssetId,
    string AssetNumber,
    string Name,
    string? Category,
    string? Supplier,
    DateTime Date,
    bool DateIsPurchase,
    decimal? Cost,
    bool Used,
    DateTime? FirstUsedOn,
    int? DaysToFirstUse,
    string? CurrentCustodian,
    LifecycleStage Stage)
{
    /// <summary>What the date column means: a real purchase date, or the registration date.</summary>
    public string DateBasis => DateIsPurchase ? "Purchased" : "Registered";
}

/// <summary>
/// One consumable item received inside the period.
/// Consumed is what left the stores in the same period, not necessarily out of this batch.
/// </summary>
public record PurchasedItem(
    int ItemId,
    string Name,
    string? Unit,
    decimal Received,
    decimal Consumed);

/// <summary>Everything the purchases report shows, already totalled.</summary>
public record PurchaseReport(
    DateTime From,
    DateTime To,
    List<PurchasedAsset> Assets,
    List<PurchasedItem> Items)
{
    public static PurchaseReport Empty(DateTime from, DateTime to) =>
        new(from, to, new List<PurchasedAsset>(), new List<PurchasedItem>());

    public int AssetsUsed => Assets.Count(a => a.Used);

    public int AssetsUnused => Assets.Count - AssetsUsed;

    public decimal CostTotal => Assets.Sum(a => a.Cost ?? 0m);

    public decimal CostUsed => Assets.Where(a => a.Used).Sum(a => a.Cost ?? 0m);

    public decimal CostUnused => Assets.Where(a => !a.Used).Sum(a => a.Cost ?? 0m);

    public decimal ItemsReceived => Items.Sum(i => i.Received);

    public decimal ItemsConsumed => Items.Sum(i => i.Consumed);

    /// <summary>Assets reported by their registration date because no purchase date was entered.</summary>
    public int AssetsWithoutPurchaseDate => Assets.Count(a => !a.DateIsPurchase);

    /// <summary>Assets with no cost on record - they count in, but add nothing to the totals.</summary>
    public int AssetsWithoutCost => Assets.Count(a => a.Cost is null);
}

/// <summary>Reports read the database and change nothing.</summary>
public interface IReportService
{
    /// <summary>
    /// What was bought between the two dates (inclusive), and whether it was ever put to use:
    /// an asset counts as used once it has one approved custody, an item once some of it left the store.
    /// </summary>
    Task<PurchaseReport> GetPurchasesAsync(DateTime from, DateTime to);
}
