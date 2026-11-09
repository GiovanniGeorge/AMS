using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Application.Common;

namespace AMS.Application.Inventory;

/// <summary>One line of a delivery: either a consumable item with a quantity, or one asset.</summary>
public record ReceiveLineInput(int? ItemId, int? AssetId, decimal Quantity);

/// <summary>What the user fills in on the "Receive" screen.</summary>
public record ReceiveInput(
    DateTime Date,
    int StoreId,
    int? SupplierId,
    int? PurchaseOrderId,
    string? Notes,
    List<ReceiveLineInput> Lines);
