using System;
using AMS.Domain.Common;

namespace AMS.Domain.Entities.Main;

public class Unit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}