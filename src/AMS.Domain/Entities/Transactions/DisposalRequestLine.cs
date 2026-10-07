using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

public class DisposalRequestLine : BaseEntity
{
    public int DisposalRequestId { get; set; }

    [ForeignKey(nameof(DisposalRequestId))]
    public virtual DisposalRequest? DisposalRequest { get; set; }

    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }
}
