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

/// <summary>
/// Supplier invoices and their lines. No approval is involved.
/// Deactivate / Activate still use ICrudService&lt;Invoice&gt;.
/// </summary>
public interface IInvoiceService
{
    Task<Paged<Invoice>> GetAllAsync(bool includeInactive = false, int page = 1);

    Task<Invoice?> GetByIdAsync(int id);

    /// <summary>The invoices that bill this asset, newest first. Usually none or one.</summary>
    Task<List<Invoice>> GetForAssetAsync(int assetId);

    /// <summary>Assets that can still be billed: active, and not already on an invoice line.</summary>
    Task<List<Asset>> GetBillableAssetsAsync(int? includeAssetId = null);

    Task<int> CreateAsync(InvoiceInput input);

    /// <summary>Replaces the header and the lines.</summary>
    Task UpdateAsync(int id, InvoiceInput input);

    /// <summary>
    /// Puts one asset on an invoice, creating the invoice when no existing one is given.
    /// Returns the invoice id.
    /// </summary>
    Task<int> RecordAssetPurchaseAsync(AssetPurchaseInput input);
}
