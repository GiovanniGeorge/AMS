using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

/// <summary>
/// A single approval decision. AMS has ONE approver and no approval levels,
/// so each transaction that needs approval points at its own Approval row
/// through its ApprovalId foreign key.
/// </summary>
public class Approval : BaseEntity
{
    public int? ApproverId { get; set; }

    [ForeignKey(nameof(ApproverId))]
    public virtual User? Approver { get; set; }

    public string? Decision { get; set; }
    public string? Comments { get; set; }
    public DateTime? DecisionDate { get; set; }
    public string? Status { get; set; }
}
