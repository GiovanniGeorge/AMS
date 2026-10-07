using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Enums;
using AMS.Domain.Entities.Transactions;

namespace AMS.Domain.Entities.Main;

public class WorkOrder : BaseEntity
{
    public int? AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }
    public string? Problem { get; set; }
    public MaintenanceType? MaintenanceType { get; set; }
    public int? TechnicianId { get; set; }

    [ForeignKey(nameof(TechnicianId))]
    public virtual Employee? Technician { get; set; }
    public int? VendorId { get; set; }

    [ForeignKey(nameof(VendorId))]
    public virtual Vendor? Vendor { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? WorkDone { get; set; }
    public string? PartsUsed { get; set; }
    public decimal? Cost { get; set; }
    public decimal? Downtime { get; set; }
    public string? Status { get; set; }
    public string? Assignment { get; set; }

    /// <summary>Set when the work order was opened by the administrator from an incident.</summary>
    public int? IncidentId { get; set; }

    [ForeignKey(nameof(IncidentId))]
    public virtual Incident? Incident { get; set; }

    /// <summary>Set when the work order came out of a preventive maintenance plan.</summary>
    public int? PreventiveMaintenancePlanId { get; set; }

    [ForeignKey(nameof(PreventiveMaintenancePlanId))]
    public virtual PreventiveMaintenancePlan? PreventiveMaintenancePlan { get; set; }
}
