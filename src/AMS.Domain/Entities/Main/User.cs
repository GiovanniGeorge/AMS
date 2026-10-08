using System;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Common;

namespace AMS.Domain.Entities.Main;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>The person this login belongs to. Optional: a service account has none.</summary>
    public int? EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee? Employee { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
