using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public virtual Category? Parent { get; set; }
    public string? DepreciationMethod { get; set; }
    public decimal? UsefulLife { get; set; }
    public decimal? SalvageValue { get; set; }
    public string? GLCode { get; set; }
    public string? NumberingPrefix { get; set; }
}