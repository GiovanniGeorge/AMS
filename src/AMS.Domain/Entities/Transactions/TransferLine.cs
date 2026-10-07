using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

public class TransferLine : BaseEntity
{
    public int TransferId { get; set; }

    [ForeignKey(nameof(TransferId))]
    public virtual Transfer? Transfer { get; set; }

    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }
}
