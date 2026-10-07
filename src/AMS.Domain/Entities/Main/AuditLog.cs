using System;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Enums;

namespace AMS.Domain.Entities.Main;

/// <summary>
/// Audit trail. Deliberately does NOT inherit BaseEntity: a log row is never
/// deactivated, never modified, and already carries its own user + timestamp.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    public AuditAction? Action { get; set; }
    public string? EntityName { get; set; }

    /// <summary>Key of the affected row. Null for Login / Logout / Failed Login.</summary>
    public string? RecordId { get; set; }

    public DateTime? DateTime { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? LogLevel { get; set; }
}
