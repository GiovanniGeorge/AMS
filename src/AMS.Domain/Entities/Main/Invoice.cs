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
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
}
