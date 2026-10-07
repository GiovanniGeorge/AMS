using System;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Common;

namespace AMS.Domain.Entities.Main;

/// <summary>Join entity for the User / Role many-to-many relationship.</summary>
public class UserRole : BaseEntity
{
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    public int RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public virtual Role? Role { get; set; }
}
