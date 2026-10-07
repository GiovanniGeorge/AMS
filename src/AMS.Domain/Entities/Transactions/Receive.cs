using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

public class Receive : BaseEntity
{
    public DateTime? Date { get; set; }
    public int? SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }
    public int? PurchaseOrderId { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }
    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public virtual Store? Store { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }

    public virtual ICollection<ReceiveLine> Lines { get; set; } = new List<ReceiveLine>();
}
