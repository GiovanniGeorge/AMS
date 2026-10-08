using Microsoft.EntityFrameworkCore;
using AMS.Application.Inventory;
using AMS.Application.Reports;
using AMS.Domain.Common;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PurchaseReport> GetPurchasesAsync(DateTime from, DateTime to)
    {
        // Both ends are inclusive: a purchase stamped at 14:30 on the "to" day still counts.
        var start = from.Date;
        var end = to.Date.AddDays(1).AddTicks(-1);

        if (start > end)
        {
            throw new InvalidOperationException("The start date must come before the end date.");
        }

        return new PurchaseReport(
            start,
            to.Date,
            await PurchasedAssetsAsync(start, end),
            await PurchasedItemsAsync(start, end));
    }

    /// <summary>
    /// Everything that entered the books inside the period: assets whose purchase date falls
    /// in it, plus assets with no purchase date that were registered in it - otherwise an asset
    /// entered without a date would never appear in any report at all.
    /// An asset counts as used once it has one approved custody; the first of those is its first use.
    /// </summary>
    private async Task<List<PurchasedAsset>> PurchasedAssetsAsync(DateTime start, DateTime end)
    {
        var assets = await _db.Assets
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Supplier)
            .Include(a => a.Custodian)
            .Where(a => a.IsActive
                        && ((a.PurchaseDate != null && a.PurchaseDate >= start && a.PurchaseDate <= end)
                            || (a.PurchaseDate == null && a.CreatedOn >= start && a.CreatedOn <= end)))
            .OrderBy(a => a.PurchaseDate ?? a.CreatedOn)
            .ThenBy(a => a.AssetNumber)
            .ToListAsync();

        if (assets.Count == 0)
        {
            return new List<PurchasedAsset>();
        }

        var ids = assets.Select(a => a.Id).ToList();

        // First approved custody per asset, whenever it happened - an asset bought in March
        // and first issued in July is still "used", just late.
        var firstUse = await _db.AssetAssignments
            .AsNoTracking()
            .Where(a => a.IsActive
                        && a.Status == RequestStatus.Approved
                        && ids.Contains(a.AssetId))
            .GroupBy(a => a.AssetId)
            .Select(g => new { AssetId = g.Key, First = g.Min(a => a.AssignmentDate) })
            .ToListAsync();

        var used = firstUse.ToDictionary(f => f.AssetId, f => f.First);

        return assets.Select(a =>
        {
            var isUsed = used.TryGetValue(a.Id, out var on);
            var entered = a.PurchaseDate ?? a.CreatedOn;

            // Days idle is counted from whichever date the row is reported under.
            int? days = isUsed && on is { } date
                ? (int)Math.Max(0, (date.Date - entered.Date).TotalDays)
                : null;

            return new PurchasedAsset(
                a.Id,
                a.AssetNumber,
                a.Name,
                a.Category?.Name,
                a.Supplier?.Name,
                entered,
                a.PurchaseDate is not null,
                a.PurchaseCost,
                isUsed,
                isUsed ? on : null,
                days,
                a.Custodian?.Name,
                a.LifecycleStage);
        }).ToList();
    }

    /// <summary>
    /// Consumable items received inside the period, against what left the stores in the
    /// same period. Stock is not tracked batch by batch, so the two numbers are a
    /// comparison over the period, not a trace of one delivery.
    /// </summary>
    private async Task<List<PurchasedItem>> PurchasedItemsAsync(DateTime start, DateTime end)
    {
        // The Select below names every column that is read, so no Include is needed.
        var received = await _db.ReceiveLines
            .AsNoTracking()
            .Where(l => l.IsActive
                        && l.ItemId != null
                        && l.Receive != null
                        && l.Receive.IsActive
                        && l.Receive.Date != null
                        && l.Receive.Date >= start
                        && l.Receive.Date <= end)
            .GroupBy(l => new
            {
                ItemId = l.ItemId!.Value,
                Name = l.Item!.Name,
                Unit = l.Item.UnitOfMeasure != null ? l.Item.UnitOfMeasure.Name : null
            })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.Name,
                g.Key.Unit,
                Quantity = g.Sum(l => l.Quantity)
            })
            .ToListAsync();

        if (received.Count == 0)
        {
            return new List<PurchasedItem>();
        }

        var ids = received.Select(r => r.ItemId).ToList();

        var consumed = await _db.StockAdjustments
            .AsNoTracking()
            .Where(s => s.IsActive
                        && s.ItemId != null
                        && ids.Contains(s.ItemId.Value)
                        && s.AdjustmentType == StockDirection.Decrease
                        && s.Date != null
                        && s.Date >= start
                        && s.Date <= end)
            .GroupBy(s => s.ItemId!.Value)
            // Decrease rows are stored signed (negative), so the absolute value is what left.
            .Select(g => new { ItemId = g.Key, Quantity = g.Sum(s => s.Quantity ?? 0m) })
            .ToListAsync();

        var out_ = consumed.ToDictionary(c => c.ItemId, c => Math.Abs(c.Quantity));

        return received
            .Select(r => new PurchasedItem(
                r.ItemId,
                r.Name,
                r.Unit,
                r.Quantity,
                out_.TryGetValue(r.ItemId, out var q) ? q : 0m))
            .OrderBy(r => r.Name)
            .ToList();
    }
}
