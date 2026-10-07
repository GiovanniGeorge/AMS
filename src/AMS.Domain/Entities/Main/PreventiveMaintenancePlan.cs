using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

public class PreventiveMaintenancePlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int? AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }
    public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }
    public string? RepeatSchedule { get; set; }
    public string? Frequency { get; set; }
    public decimal? UsageThreshold { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
