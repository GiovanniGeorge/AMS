using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

public class VerificationCampaign : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int? LocationId { get; set; }

    [ForeignKey(nameof(LocationId))]
    public virtual Location? Location { get; set; }
    public int? DepartmentId { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public virtual Department? Department { get; set; }
    public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public string? ExpectedSnapshot { get; set; }
    public decimal? Progress { get; set; }
    public string? Results { get; set; }
}
