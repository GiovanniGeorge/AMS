using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

/// <summary>
/// Historical depreciation record for one asset for one period.
/// Stored (not recalculated) so DisposalRequest.NetBookValue / GainLoss can be
/// taken from the latest entry. One entry per asset per period.
/// </summary>
public class DepreciationEntry : BaseEntity
{
    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }

    public DateTime PeriodDate { get; set; }

    /// <summary>Depreciation charged for this period.</summary>
    public decimal Amount { get; set; }

    /// <summary>Total depreciation to date, after this entry.</summary>
    public decimal AccumulatedDepreciation { get; set; }

    /// <summary>Book value after this entry.</summary>
    public decimal NetBookValue { get; set; }

    public string? Notes { get; set; }
}
