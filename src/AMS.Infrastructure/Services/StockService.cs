using Microsoft.EntityFrameworkCore;
using AMS.Application.Inventory;
using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Infrastructure.Persistence;
using AMS.Application.Common;

namespace AMS.Infrastructure.Services;

/// <summary>
/// Consumable items in stores. Items never go out through an Issue (that is for assets only):
/// they leave through a stock adjustment with a reason.
/// Balances are kept in StockBalance, one row per (store, item).
/// </summary>
public class StockService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public StockService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<StockBalance>> GetBalancesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.StockBalances
            .AsNoTracking()
            .Include(b => b.Store)
            .Include(b => b.Item!).ThenInclude(i => i.UnitOfMeasure)
            .Where(b => b.IsActive)
            .OrderBy(b => b.Store!.Name)
            .ThenBy(b => b.Item!.Name)
            .ToListAsync();
    }

    public async Task<Paged<StockAdjustment>> GetAdjustmentsAsync(int page = 1)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = db.StockAdjustments
            .AsNoTracking()
            .Include(a => a.Store)
            .Include(a => a.Item)
            .Where(a => a.IsActive);

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(a => a.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<StockAdjustment>.Size)
            .Take(Paged<StockAdjustment>.Size)
            .ToListAsync();

        return new Paged<StockAdjustment>(items, total);
    }

    /// <summary>The current quantity of one item in one store (0 when there is no row yet).</summary>
    public async Task<decimal> GetQuantityAsync(int storeId, int itemId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.StockBalances
            .AsNoTracking()
            .Where(b => b.IsActive && b.StoreId == storeId && b.ItemId == itemId)
            .Select(b => b.Quantity)
            .FirstOrDefaultAsync();
    }

    /// <summary>Applies the adjustment at once and moves the balance. Stock cannot go below zero.</summary>
    public async Task<int> AdjustAsync(StockAdjustmentInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (input.Quantity <= 0)
        {
            throw new InvalidOperationException("The quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(input.Reason))
        {
            throw new InvalidOperationException("A reason is required for every stock adjustment.");
        }

        if (input.Direction != StockDirection.Increase && input.Direction != StockDirection.Decrease)
        {
            throw new InvalidOperationException("The direction must be Increase or Decrease.");
        }

        var signed = input.Direction == StockDirection.Increase ? input.Quantity : -input.Quantity;

        var balance = await db.StockBalances
            .FirstOrDefaultAsync(b => b.StoreId == input.StoreId && b.ItemId == input.ItemId);

        var current = balance?.Quantity ?? 0m;
        if (current + signed < 0)
        {
            throw new InvalidOperationException(
                $"There is not enough stock: the store holds {current:N2} of this item.");
        }

        if (balance is null)
        {
            db.StockBalances.Add(new StockBalance
            {
                StoreId = input.StoreId,
                ItemId = input.ItemId,
                Quantity = signed
            });
        }
        else
        {
            balance.Quantity = current + signed;
            balance.IsActive = true;
        }

        var adjustment = new StockAdjustment
        {
            StoreId = input.StoreId,
            ItemId = input.ItemId,
            Quantity = signed,
            AdjustmentType = input.Direction,
            Reason = input.Reason.Trim(),
            Date = input.Date,
            Status = "Applied"
        };

        db.StockAdjustments.Add(adjustment);
        await db.SaveChangesAsync();
        return adjustment.Id;
    }
}
