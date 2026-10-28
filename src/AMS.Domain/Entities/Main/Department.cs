using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

public class Department : BaseEntity, INamedEntity
{
    public string Name { get; set; } = string.Empty;
    public int? ParentDepartmentId { get; set; }

    [ForeignKey(nameof(ParentDepartmentId))]
    public virtual Department? ParentDepartment { get; set; }
}