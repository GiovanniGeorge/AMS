using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

public class Location : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public virtual Location? Parent { get; set; }
    public string? Level { get; set; }
    public string? Type { get; set; }
}