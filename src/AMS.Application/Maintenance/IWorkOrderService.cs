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

/// <summary>
/// Maintenance work on an asset. Opening a work order puts the asset Under maintenance;
/// closing it sends the asset back In store and clears its custodian.
/// </summary>
public interface IWorkOrderService
{
    Task<Paged<WorkOrder>> GetAllAsync(string? status = null, int page = 1);

    /// <summary>Assets a work order can be opened for: active, not disposed, not already in maintenance or in transit.</summary>
    Task<List<Asset>> GetMaintainableAssetsAsync();

    /// <summary>
    /// <paramref name="incidentId"/> is filled in when the administrator opened it from an incident,
    /// <paramref name="planId"/> when it came from a preventive maintenance plan.
    /// </summary>
    Task<int> CreateAsync(WorkOrderInput input, int? incidentId = null, int? planId = null);

    /// <summary>Closes the work order: the asset returns In store and its custodian is cleared.</summary>
    Task CloseAsync(WorkOrderClosure closure);
}
