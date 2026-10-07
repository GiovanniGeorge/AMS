using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

public class PurchaseOrder : BaseEntity
{
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public int? SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Status { get; set; }
    public decimal? TotalCost { get; set; }
    public string? Currency { get; set; }

    public virtual ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
