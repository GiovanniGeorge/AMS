using AMS.Domain.Entities.Main;
using AMS.Application.Common;

namespace AMS.Application.Procurement;

/// <summary>
/// One line of an invoice. It bills exactly one of three things: an item from the
/// store catalogue, a registered asset, or free text for anything else.
/// </summary>
public record InvoiceLineInput(
    int? ItemId,
    string? Description,
    decimal Quantity,
    decimal? UnitPrice,
    int? AssetId = null);

/// <summary>
/// What the user fills in on the "Invoice" screen.
/// The purchase order is optional: plenty is bought without one.
/// <paramref name="Amount"/> is only used when there are no lines; with lines the
/// amount is their sum.
/// </summary>
public record InvoiceInput(
    string InvoiceNumber,
    int? SupplierId,
    int? PurchaseOrderId,
    DateTime? InvoiceDate,
    decimal? Amount,
    string? Currency,
    List<InvoiceLineInput> Lines);

/// <summary>
/// Recording the purchase of one asset: either onto an invoice that already exists
/// (<paramref name="InvoiceId"/>) or onto a new one raised from these details.
/// </summary>
public record AssetPurchaseInput(
    int AssetId,
    int? InvoiceId,
    string? InvoiceNumber,
    int? SupplierId,
    DateTime? InvoiceDate,
    decimal? Amount,
    string? Currency);
