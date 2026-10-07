using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Entities.Main;

namespace AMS.Domain.Entities.Transactions;

/// <summary>
/// Asset Issuance: an asset leaving a Store for an employee.
/// It opens an AssetAssignment (the custody period); the approval is on that assignment.
/// Consumable items are not issued, so there is no ItemId / Quantity.
/// </summary>
public class Issue : BaseEntity
{
    public DateTime? Date { get; set; }

    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }

    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee? Employee { get; set; }

    /// <summary>The store the asset leaves.</summary>
    public int StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public virtual Store? Store { get; set; }

    public string? Status { get; set; }
}
