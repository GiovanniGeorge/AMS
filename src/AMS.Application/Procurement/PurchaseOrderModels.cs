using AMS.Domain.Entities.Main;
using AMS.Application.Common;

namespace AMS.Application.Procurement;

/// <summary>One line of a purchase order: an item from the catalogue, or free text.</summary>
public record PurchaseOrderLineInput(int? ItemId, string? Description, decimal Quantity, decimal? UnitPrice);

/// <summary>
/// What the user fills in on the "Purchase order" screen.
/// The number is not here: it is generated, never typed.
/// Lines may be empty - an order can be opened first and filled in later.
/// </summary>
public record PurchaseOrderInput(
    int? SupplierId,
    DateTime? PurchaseDate,
    DateTime? ExpectedDeliveryDate,
    string? Currency,
    List<PurchaseOrderLineInput> Lines);

/// <summary>Status values used by PurchaseOrder.</summary>
public static class PurchaseOrderStatus
{
    public const string Open = "Open";
    public const string Closed = "Closed";
}
