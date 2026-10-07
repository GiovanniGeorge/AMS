using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

/// <summary>Exactly one of ItemId / AssetId is populated (enforced by CK_ReceiveLine_AssetOrItem).</summary>
public class ReceiveLine : BaseEntity
{
    public int ReceiveId { get; set; }

    [ForeignKey(nameof(ReceiveId))]
    public virtual Receive? Receive { get; set; }

    public int? ItemId { get; set; }

    [ForeignKey(nameof(ItemId))]
    public virtual Item? Item { get; set; }

    public int? AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }

    public decimal Quantity { get; set; }
}
