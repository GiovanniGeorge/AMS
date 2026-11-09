using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;
using AMS.Application.Common;

namespace AMS.Application.Maintenance;

/// <summary>What the user fills in when opening a work order.</summary>
public record WorkOrderInput(
    int AssetId,
    MaintenanceType MaintenanceType,
    string? Problem,
    int? TechnicianId,
    int? VendorId,
    DateTime StartDate);

/// <summary>What the user fills in when closing a work order.</summary>
public record WorkOrderClosure(
    int WorkOrderId,
    string? WorkDone,
    string? PartsUsed,
    decimal? Cost,
    decimal? Downtime,
    DateTime EndDate);

/// <summary>Status values used by WorkOrder.</summary>
public static class WorkOrderStatus
{
    public const string Open = "Open";
    public const string Closed = "Closed";
}
