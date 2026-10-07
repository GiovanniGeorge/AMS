using AMS.Domain.Entities.Main;

namespace AMS.Application.Maintenance;

/// <summary>How often a preventive plan comes round. Fixed list, so a drop-down.</summary>
public static class PlanFrequency
{
    public const string Weekly = "Weekly";
    public const string Monthly = "Monthly";
    public const string Quarterly = "Quarterly";
    public const string SemiAnnual = "Semi-annual";
    public const string Annual = "Annual";

    public static readonly string[] All = { Weekly, Monthly, Quarterly, SemiAnnual, Annual };

    /// <summary>The length of one cycle in days. Used only to work out the next due date.</summary>
    public static int Days(string? frequency) => frequency switch
    {
        Weekly => 7,
        Monthly => 30,
        Quarterly => 91,
        SemiAnnual => 182,
        Annual => 365,
        _ => 0
    };
}

/// <summary>What the user fills in on the "Preventive maintenance plan" screen.</summary>
public record MaintenancePlanInput(
    string Name,
    int? AssetId,
    int? CategoryId,
    string Frequency,
    string? ScheduleNotes,
    decimal? UsageThreshold,
    DateTime? StartDate,
    DateTime? EndDate);

/// <summary>One asset a plan says is due now, and why.</summary>
public record DuePlanItem(
    int PlanId,
    string PlanName,
    int AssetId,
    string AssetNumber,
    string AssetName,
    string Frequency,
    DateTime? LastDone,
    DateTime DueOn);

/// <summary>
/// Preventive maintenance plans. A plan covers one asset or a whole category.
/// Nothing happens on its own: the screen lists what is due now and the user opens
/// the work order with a button.
/// </summary>
public interface IMaintenancePlanService
{
    Task<List<PreventiveMaintenancePlan>> GetAllAsync();

    Task<int> CreateAsync(MaintenancePlanInput input);

    Task UpdateAsync(int id, MaintenancePlanInput input);

    /// <summary>
    /// Everything due today or earlier: for each asset in each active plan, the last
    /// preventive work order that plan produced plus one cycle.
    /// </summary>
    Task<List<DuePlanItem>> GetDueAsync();

    /// <summary>Opens a preventive work order for that asset and plan. The asset goes Under maintenance.</summary>
    Task<int> OpenWorkOrderAsync(int planId, int assetId);
}
