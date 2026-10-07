using System;
using AMS.Domain.Common;

namespace AMS.Domain.Entities.Main;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
