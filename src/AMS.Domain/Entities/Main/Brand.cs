using System;
using AMS.Domain.Common;

namespace AMS.Domain.Entities.Main;

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}