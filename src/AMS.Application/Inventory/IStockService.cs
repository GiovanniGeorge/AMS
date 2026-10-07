using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Application.Common;

namespace AMS.Application.Inventory;

/// <summary>The two directions a stock adjustment can take (stored in StockAdjustment.AdjustmentType).</summary>
public static class StockDirection
{
    /// <summary>Stock goes up: a correction after a count, something found again.</summary>
    public const string Increase = "Increase";

    /// <summary>Stock goes down: consumed, damaged, a correction after a count.</summary>
    public const string Decrease = "Decrease";
}

/// <summary>What the user fills in on the "Stock adjustment" screen. The reason is required.</summary>
public record StockAdjustmentInput(
    int StoreId,
    int ItemId,
    decimal Quantity,
    string Direction,
    string Reason,
    DateTime Date);

/// <summary>
/// Consumable items in stores. Items never go out through an Issue (that is for assets only):
/// they leave through a stock adjustment with a reason.
/// Balances are kept in StockBalance, one row per (store, item).
/// </summary>
public interface IStockService
{
    Task<List<StockBalance>> GetBalancesAsync();

    Task<Paged<StockAdjustment>> GetAdjustmentsAsync(int page = 1);

    /// <summary>The current quantity of one item in one store (0 when there is no row yet).</summary>
    Task<decimal> GetQuantityAsync(int storeId, int itemId);

    /// <summary>Applies the adjustment at once and moves the balance. Stock cannot go below zero.</summary>
    Task<int> AdjustAsync(StockAdjustmentInput input);
}
