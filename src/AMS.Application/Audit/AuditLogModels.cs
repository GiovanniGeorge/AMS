using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;

namespace AMS.Application.Audit;

/// <summary>Filters for the audit log screen. Everything is optional.</summary>
public record AuditFilter(
    AuditAction? Action = null,
    string? EntityName = null,
    int? UserId = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50);

/// <summary>One page of audit entries plus the total number of matching rows.</summary>
public record AuditPage(List<AuditLog> Items, int Total);
