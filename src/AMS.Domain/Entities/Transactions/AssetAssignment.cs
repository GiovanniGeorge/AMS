using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

/// <summary>
/// The custody period of an asset: who holds it, in which department and location,
/// from AssignmentDate until ReturnDate. Opened by an Issue; this is the record that is approved.
/// </summary>
public class AssetAssignment : BaseEntity
{
    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }

    /// <summary>The employee who holds the asset.</summary>
    public int CustodianId { get; set; }

    [ForeignKey(nameof(CustodianId))]
    public virtual Employee? Custodian { get; set; }

    public int? DepartmentId { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public virtual Department? Department { get; set; }

    public int? LocationId { get; set; }

    [ForeignKey(nameof(LocationId))]
    public virtual Location? Location { get; set; }

    public DateTime? AssignmentDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }

    public int? ApprovalId { get; set; }

    [ForeignKey(nameof(ApprovalId))]
    public virtual Approval? Approval { get; set; }

    /// <summary>The issuance that opened this custody (empty when opened by a Transfer later).</summary>
    public int? IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public virtual Issue? Issue { get; set; }
}
