using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

public class PurchaseOrderLine : BaseEntity
{
    public int PurchaseOrderId { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    /// <summary>Optional: a PO line may be for a catalogue Item or for free-text goods.</summary>
    public int? ItemId { get; set; }

    [ForeignKey(nameof(ItemId))]
    public virtual Item? Item { get; set; }

    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }

    // Line total is intentionally not stored: it is Quantity * UnitPrice.
}
