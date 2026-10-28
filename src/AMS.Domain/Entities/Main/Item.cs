using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

public class Item : BaseEntity, INamedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UnitOfMeasureId { get; set; }

    [ForeignKey(nameof(UnitOfMeasureId))]
    public virtual Unit? UnitOfMeasure { get; set; }
    public decimal? MinimumLevel { get; set; }
    public decimal? ReorderPoint { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
