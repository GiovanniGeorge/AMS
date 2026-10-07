using Microsoft.EntityFrameworkCore;
using AMS.Application.Assets;
using AMS.Application.Common;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class DepreciationService : IDepreciationService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DepreciationService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Paged<DepreciationEntry>> GetAllAsync(int page = 1)
    {
        var query = _db.DepreciationEntries
            .AsNoTracking()
            .Include(e => e.Asset)
            .Where(e => e.IsActive);

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(e => e.PeriodDate).ThenBy(e => e.AssetId)
            .Skip((Math.Max(page, 1) - 1) * Paged<DepreciationEntry>.Size)
            .Take(Paged<DepreciationEntry>.Size)
            .ToListAsync();

        return new Paged<DepreciationEntry>(items, total);
    }

    public async Task<List<DepreciationPreviewLine>> PreviewAsync(string period, DateTime periodEnd)
    {
        return await BuildAsync(period, periodEnd);
    }

    public async Task<int> PostAsync(string period, DateTime periodEnd)
    {
        if (!await _currentUser.IsInRoleAsync(AppRoles.Admin))
        {
            throw new InvalidOperationException("Only the administrator can post depreciation.");
        }

        var lines = await BuildAsync(period, periodEnd);
        var postable = lines.Where(l => l.Skipped is null).ToList();

        if (postable.Count == 0)
        {
            throw new InvalidOperationException("There is nothing to post for this period.");
        }

        try
        {
            foreach (var line in postable)
            {
                _db.DepreciationEntries.Add(new DepreciationEntry
                {
                    AssetId = line.AssetId,
                    PeriodDate = periodEnd.Date,
                    Amount = line.Amount,
                    AccumulatedDepreciation = line.AccumulatedDepreciation,
                    NetBookValue = line.NetBookValue,
                    Notes = period
                });
            }

            await _db.SaveChangesAsync();
            return postable.Count;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    // ------------------------------------------------------------------
    // The calculation
    // ------------------------------------------------------------------

    private async Task<List<DepreciationPreviewLine>> BuildAsync(string period, DateTime periodEnd)
    {
        if (period != DepreciationPeriod.Monthly && period != DepreciationPeriod.Annual)
        {
            throw new InvalidOperationException("The period must be Monthly or Annual.");
        }

        if (periodEnd == default)
        {
            throw new InvalidOperationException("Choose the date the period ends on.");
        }

        var date = periodEnd.Date;

        var assets = await _db.Assets
            .AsNoTracking()
            .Where(a => a.IsActive && a.LifecycleStage != LifecycleStage.Disposed)
            .Select(a => new
            {
                a.Id,
                a.AssetNumber,
                a.Name,
                a.PurchaseCost,
                a.PurchaseDate,
                UsefulLife = a.Category!.UsefulLife,
                Salvage = a.Category.SalvageValue
            })
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();

        // Everything already posted for these assets, so the run can carry on from it.
        var posted = await _db.DepreciationEntries
            .AsNoTracking()
            .Where(e => e.IsActive)
            .Select(e => new { e.AssetId, e.PeriodDate, e.AccumulatedDepreciation })
            .ToListAsync();

        var lines = new List<DepreciationPreviewLine>();

        foreach (var asset in assets)
        {
            var cost = asset.PurchaseCost ?? 0m;
            var salvage = asset.Salvage ?? 0m;
            if (salvage > cost)
            {
                salvage = cost;
            }

            string? skip = null;

            if (asset.PurchaseCost is null || cost <= 0)
            {
                skip = "no purchase cost";
            }
            else if (asset.UsefulLife is not decimal life || life <= 0)
            {
                skip = "the category has no useful life";
            }
            else if (asset.PurchaseDate is null)
            {
                skip = "no purchase date";
            }
            else if (asset.PurchaseDate.Value.Date > date)
            {
                skip = "bought after this period";
            }

            var already = posted.Where(e => e.AssetId == asset.Id).ToList();

            if (skip is null && already.Any(e => e.PeriodDate.Date == date))
            {
                skip = "already posted for this period";
            }

            if (skip is not null)
            {
                lines.Add(new DepreciationPreviewLine(asset.Id, asset.AssetNumber, asset.Name, cost, 0, 0, 0, skip));
                continue;
            }

            var years = asset.UsefulLife!.Value;
            var annual = (cost - salvage) / years;
            var amount = period == DepreciationPeriod.Annual ? annual : annual / 12m;

            var accumulatedBefore = already.Count == 0
                ? 0m
                : already.OrderByDescending(e => e.PeriodDate).First().AccumulatedDepreciation;

            // Never depreciate past the salvage value.
            var room = cost - salvage - accumulatedBefore;
            if (room <= 0)
            {
                lines.Add(new DepreciationPreviewLine(asset.Id, asset.AssetNumber, asset.Name, cost,
                    0, accumulatedBefore, cost - accumulatedBefore, "fully depreciated"));
                continue;
            }

            if (amount > room)
            {
                amount = room;
            }

            amount = decimal.Round(amount, 2);
            var accumulated = decimal.Round(accumulatedBefore + amount, 2);

            lines.Add(new DepreciationPreviewLine(
                asset.Id, asset.AssetNumber, asset.Name, cost,
                amount, accumulated, decimal.Round(cost - accumulated, 2), null));
        }

        return lines;
    }
}
