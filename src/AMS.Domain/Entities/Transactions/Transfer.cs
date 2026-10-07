using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

public class Transfer : BaseEntity
{
    public int? FromLocationId { get; set; }

    [ForeignKey(nameof(FromLocationId))]
    public virtual Location? FromLocation { get; set; }
    public int? FromDepartmentId { get; set; }

    [ForeignKey(nameof(FromDepartmentId))]
    public virtual Department? FromDepartment { get; set; }
    public int? FromCustodianId { get; set; }

    [ForeignKey(nameof(FromCustodianId))]
    public virtual Employee? FromCustodian { get; set; }
    public int? ToLocationId { get; set; }

    [ForeignKey(nameof(ToLocationId))]
    public virtual Location? ToLocation { get; set; }
    public int? ToDepartmentId { get; set; }

    [ForeignKey(nameof(ToDepartmentId))]
    public virtual Department? ToDepartment { get; set; }
    public int? ToCustodianId { get; set; }

    [ForeignKey(nameof(ToCustodianId))]
    public virtual Employee? ToCustodian { get; set; }
    public DateTime? Date { get; set; }
    public string? Status { get; set; }
    public int? ApprovalId { get; set; }

    [ForeignKey(nameof(ApprovalId))]
    public virtual Approval? Approval { get; set; }
    public DateTime? DispatchDate { get; set; }
    public DateTime? ReceiveConfirmationDate { get; set; }

    public virtual ICollection<TransferLine> Lines { get; set; } = new List<TransferLine>();
}
