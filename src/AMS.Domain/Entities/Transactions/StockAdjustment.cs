using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

public class StockAdjustment : BaseEntity
{
    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public virtual Store? Store { get; set; }
    public int? ItemId { get; set; }

    [ForeignKey(nameof(ItemId))]
    public virtual Item? Item { get; set; }
    public decimal? Quantity { get; set; }
    public string? AdjustmentType { get; set; }
    public string? Reason { get; set; }
    public DateTime? Date { get; set; }
    public string? Status { get; set; }
    public int? ApprovalId { get; set; }

    [ForeignKey(nameof(ApprovalId))]
    public virtual Approval? Approval { get; set; }
}
