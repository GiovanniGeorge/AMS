using AMS.Domain.Entities.Main;
using AMS.Application.Common;

namespace AMS.Application.Procurement;

/// <summary>One line of a purchase order: an item from the catalogue, or free text.</summary>
public record PurchaseOrderLineInput(int? ItemId, string? Description, decimal Quantity, decimal? UnitPrice);

/// <summary>What the user fills in on the "Purchase order" screen.</summary>
public record PurchaseOrderInput(
    string PurchaseOrderNumber,
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

/// <summary>
/// Purchase orders and their lines. No approval is involved.
/// TotalCost is always the sum of the lines (quantity x unit price), never typed by hand.
/// </summary>
public interface IPurchaseOrderService
{
    Task<Paged<PurchaseOrder>> GetAllAsync(string? status = null, int page = 1);

    /// <summary>Open orders, for the Receive screen to pick from.</summary>
    Task<List<PurchaseOrder>> GetOpenAsync();

    Task<PurchaseOrder?> GetByIdAsync(int id);

    Task<int> CreateAsync(PurchaseOrderInput input);

    /// <summary>Replaces the header and the lines of an open order.</summary>
    Task UpdateAsync(int id, PurchaseOrderInput input);

    Task CloseAsync(int id);
}
