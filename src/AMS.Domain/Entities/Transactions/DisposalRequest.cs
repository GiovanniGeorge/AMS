using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

public class DisposalRequest : BaseEntity
{
    public string? DisposalReason { get; set; }
    public DateTime? RequestDate { get; set; }
    public int? RequestedById { get; set; }

    [ForeignKey(nameof(RequestedById))]
    public virtual User? RequestedBy { get; set; }
    public decimal? SaleProceeds { get; set; }
    public decimal? NetBookValue { get; set; }
    public decimal? GainLoss { get; set; }
    public string? Status { get; set; }
    public int? ApprovalId { get; set; }

    [ForeignKey(nameof(ApprovalId))]
    public virtual Approval? Approval { get; set; }

    /// <summary>Set when the request came out of an incident (missing / stolen, or the approver's decision).</summary>
    public int? IncidentId { get; set; }

    [ForeignKey(nameof(IncidentId))]
    public virtual Incident? Incident { get; set; }

    public virtual ICollection<DisposalRequestLine> Lines { get; set; } = new List<DisposalRequestLine>();
}
