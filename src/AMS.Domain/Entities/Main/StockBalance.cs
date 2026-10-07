using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

/// <summary>Current quantity of an Item in a Store. Assets are serialized and are not tracked here.</summary>
public class StockBalance : BaseEntity
{
    public int StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public virtual Store? Store { get; set; }

    public int ItemId { get; set; }

    [ForeignKey(nameof(ItemId))]
    public virtual Item? Item { get; set; }

    public decimal Quantity { get; set; }
}
