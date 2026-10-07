using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;

namespace AMS.Domain.Entities.Transactions;

/// <summary>
/// Something that happened to an asset: it was damaged, it stopped working, it is missing or stolen.
/// Damaged / Malfunction wait for the approver, who sends the asset either to a work order
/// or to a disposal request. Missing / Stolen need no approval: a disposal request is created at once
/// and the asset's last stage and last custodian are kept here so the approver can see them.
/// </summary>
public class Incident : BaseEntity
{
    public int? AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }
    public IncidentType? IncidentType { get; set; }
    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public int? ReportedById { get; set; }

    [ForeignKey(nameof(ReportedById))]
    public virtual User? ReportedBy { get; set; }
    public string? Status { get; set; }
    public int? ApprovalId { get; set; }

    [ForeignKey(nameof(ApprovalId))]
    public virtual Approval? Approval { get; set; }

    /// <summary>The asset's stage when the incident was reported (kept because the asset moves on afterwards).</summary>
    public LifecycleStage? LastStage { get; set; }

    /// <summary>The employee who was holding the asset when the incident was reported.</summary>
    public int? LastCustodianId { get; set; }

    [ForeignKey(nameof(LastCustodianId))]
    public virtual Employee? LastCustodian { get; set; }
}
