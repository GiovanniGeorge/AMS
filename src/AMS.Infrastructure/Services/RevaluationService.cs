using Microsoft.EntityFrameworkCore;
using AMS.Application.Transactions;
using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;
using AMS.Application.Common;

namespace AMS.Infrastructure.Services;

public class RevaluationService : IRevaluationService
{
    private readonly ApplicationDbContext _db;

    public RevaluationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Paged<RevaluationEvent>> GetAllAsync(int page = 1)
    {
        var query = _db.RevaluationEvents
            .AsNoTracking()
            .Include(r => r.Asset)
            .Where(r => r.IsActive);

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(r => r.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<RevaluationEvent>.Size)
            .Take(Paged<RevaluationEvent>.Size)
            .ToListAsync();

        return new Paged<RevaluationEvent>(items, total);
    }

    public async Task<List<Asset>> GetRevaluableAssetsAsync()
    {
        return await _db.Assets
            .AsNoTracking()
            .Where(a => a.IsActive && a.LifecycleStage != LifecycleStage.Disposed)
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    public async Task<decimal?> GetCurrentValueAsync(int assetId)
    {
        var last = await _db.RevaluationEvents
            .AsNoTracking()
            .Where(r => r.IsActive && r.AssetId == assetId)
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.Id)
            .Select(r => r.NewValue)
            .FirstOrDefaultAsync();

        if (last is not null)
        {
            return last;
        }

        return await _db.Assets
            .AsNoTracking()
            .Where(a => a.Id == assetId)
            .Select(a => a.PurchaseCost)
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateAsync(RevaluationInput input)
    {
        try
        {
            var asset = await _db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == input.AssetId)
                        ?? throw new InvalidOperationException("Asset not found.");

            if (!asset.IsActive || asset.LifecycleStage == LifecycleStage.Disposed)
            {
                throw new InvalidOperationException("A disposed or inactive asset cannot be revalued.");
            }

            var oldValue = await GetCurrentValueAsync(input.AssetId);

            var revaluation = new RevaluationEvent
            {
                AssetId = input.AssetId,
                OldValue = oldValue,
                NewValue = input.NewValue,
                Difference = input.NewValue - (oldValue ?? 0m),
                Date = input.Date,
                Reason = input.Reason,
                Status = "Recorded"
            };

            // History only: the asset's purchase cost is not touched.
            _db.RevaluationEvents.Add(revaluation);
            await _db.SaveChangesAsync();
            return revaluation.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }
}
