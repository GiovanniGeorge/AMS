using System;
using AMS.Domain.Common;
using AMS.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

public class Asset : BaseEntity
{
    public string AssetNumber { get; set; } = string.Empty;
    public string? LegacyAssetNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }
    public int? BrandId { get; set; }

    [ForeignKey(nameof(BrandId))]
    public virtual Brand? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? TagOrBarcodeNumber { get; set; }
    public int? SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public string? Currency { get; set; }
    public string? FundingSource { get; set; }
    public DateTime? WarrantyStart { get; set; }
    public DateTime? WarrantyEnd { get; set; }
    public int? LocationId { get; set; }

    [ForeignKey(nameof(LocationId))]
    public virtual Location? Location { get; set; }
    public int? DepartmentId { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public virtual Department? Department { get; set; }
    public int? CustodianId { get; set; }

    [ForeignKey(nameof(CustodianId))]
    public virtual Employee? Custodian { get; set; }
    public int StatusId { get; set; }

    [ForeignKey(nameof(StatusId))]
    public virtual AssetStatus? Status { get; set; }

    /// <summary>Lifecycle stage - changed by the system and by transactions, never typed by the user.</summary>
    public LifecycleStage LifecycleStage { get; set; } = LifecycleStage.InStore;
    public string? Condition { get; set; }
    public string? Notes { get; set; }
    public int? ParentAssetId { get; set; }

    [ForeignKey(nameof(ParentAssetId))]
    public virtual Asset? ParentAsset { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public string? Insurer { get; set; }
    public decimal? InsuredValue { get; set; }
    public DateTime? InsuranceExpiryDate { get; set; }
}
