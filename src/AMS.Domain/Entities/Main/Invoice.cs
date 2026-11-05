using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }
    public int? PurchaseOrderId { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }
    public DateTime? InvoiceDate { get; set; }

    /// <summary>
    /// What the supplier billed. Once the invoice has lines this is their sum and is
    /// no longer typed; an invoice with no lines keeps the amount entered by hand, so
    /// invoices recorded before lines existed are not rewritten to zero.
    /// </summary>
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }

    public virtual ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}
