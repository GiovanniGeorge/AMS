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
