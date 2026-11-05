using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

/// <summary>
/// One line of a supplier invoice. It bills exactly one of three things: an item from
/// the store catalogue, a registered asset, or free text when it is neither.
/// </summary>
public class InvoiceLine : BaseEntity
{
    public int InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public virtual Invoice? Invoice { get; set; }

    public int? ItemId { get; set; }

    [ForeignKey(nameof(ItemId))]
    public virtual Item? Item { get; set; }

    /// <summary>The asset this line paid for. This is what ties an asset to its invoice.</summary>
    public int? AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }

    /// <summary>Used when neither an item nor an asset is chosen, and as a note beside one when it is.</summary>
    public string? Description { get; set; }

    public decimal Quantity { get; set; }

    public decimal? UnitPrice { get; set; }
}
