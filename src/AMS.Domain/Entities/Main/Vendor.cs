using System;
using AMS.Domain.Common;

namespace AMS.Domain.Entities.Main;

public class Vendor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactDetails { get; set; }
}