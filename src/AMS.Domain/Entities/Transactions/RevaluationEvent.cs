using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

public class RevaluationEvent : BaseEntity
{
    public int? AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }
    public decimal? OldValue { get; set; }
    public decimal? NewValue { get; set; }
    public decimal? Difference { get; set; }
    public DateTime? Date { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
}
