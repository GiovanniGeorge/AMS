using Microsoft.EntityFrameworkCore;
using AMS.Application.Maintenance;
using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

/// <summary>
/// Preventive maintenance plans. A plan covers one asset or a whole category.
/// Nothing happens on its own: the screen lists what is due now and the user opens
/// the work order with a button.
/// </summary>
public class MaintenancePlanService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly WorkOrderService _workOrders;

    public MaintenancePlanService(IDbContextFactory<ApplicationDbContext> dbFactory, WorkOrderService workOrders)
    {
        _dbFactory = dbFactory;
        _workOrders = workOrders;
    }

    public async Task<List<PreventiveMaintenancePlan>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.PreventiveMaintenancePlans
            .AsNoTracking()
            .Include(p => p.Asset)
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(MaintenancePlanInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        Validate(input);

        var plan = new PreventiveMaintenancePlan
        {
            Name = input.Name.Trim(),
            AssetId = input.AssetId,
            CategoryId = input.CategoryId,
            Frequency = input.Frequency,
            RepeatSchedule = input.ScheduleNotes,
            UsageThreshold = input.UsageThreshold,
            StartDate = input.StartDate,
            EndDate = input.EndDate
        };

        db.PreventiveMaintenancePlans.Add(plan);
        await db.SaveChangesAsync();
        return plan.Id;
    }

    public async Task UpdateAsync(int id, MaintenancePlanInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        Validate(input);

        var plan = await db.PreventiveMaintenancePlans.FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
                   ?? throw new InvalidOperationException("Plan not found.");

        plan.Name = input.Name.Trim();
        plan.AssetId = input.AssetId;
        plan.CategoryId = input.CategoryId;
        plan.Frequency = input.Frequency;
        plan.RepeatSchedule = input.ScheduleNotes;
        plan.UsageThreshold = input.UsageThreshold;
        plan.StartDate = input.StartDate;
        plan.EndDate = input.EndDate;

        await db.SaveChangesAsync();
    }

    private static void Validate(MaintenancePlanInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new InvalidOperationException("The plan name is required.");
        }

        if (input.AssetId is null && input.CategoryId is null)
        {
            throw new InvalidOperationException("Choose an asset or a category for the plan.");
        }

        if (PlanFrequency.Days(input.Frequency) == 0)
        {
            throw new InvalidOperationException("Choose how often the plan repeats.");
        }
    }

    // ------------------------------------------------------------------
    // What is due now
    // ------------------------------------------------------------------

    /// <summary>
    /// Everything due today or earlier: for each asset in each active plan, the last
    /// preventive work order that plan produced plus one cycle.
    /// </summary>
    public async Task<List<DuePlanItem>> GetDueAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var today = DateTime.UtcNow.Date;

        var plans = await db.PreventiveMaintenancePlans
            .AsNoTracking()
            .Where(p => p.IsActive
                        && (p.StartDate == null || p.StartDate <= today)
                        && (p.EndDate == null || p.EndDate >= today))
            .ToListAsync();

        var due = new List<DuePlanItem>();

        foreach (var plan in plans)
        {
            var cycle = PlanFrequency.Days(plan.Frequency);
            if (cycle == 0)
            {
                continue;
            }

            var assets = await db.Assets
                .AsNoTracking()
                .Where(a => a.IsActive
                            && a.LifecycleStage != LifecycleStage.Disposed
                            && a.LifecycleStage != LifecycleStage.UnderMaintenance
                            && (plan.AssetId == null || a.Id == plan.AssetId)
                            && (plan.CategoryId == null || a.CategoryId == plan.CategoryId))
                .Select(a => new { a.Id, a.AssetNumber, a.Name })
                .ToListAsync();

            foreach (var asset in assets)
            {
                // The last work order this plan produced for this asset.
                var last = await db.WorkOrders
                    .AsNoTracking()
                    .Where(w => w.IsActive && w.AssetId == asset.Id && w.PreventiveMaintenancePlanId == plan.Id)
                    .OrderByDescending(w => w.StartDate)
                    .Select(w => w.StartDate)
                    .FirstOrDefaultAsync();

                // Never done yet: it is due from the plan's start date (or today).
                var dueOn = last is null
                    ? (plan.StartDate?.Date ?? today)
                    : last.Value.Date.AddDays(cycle);

                if (dueOn <= today)
                {
                    due.Add(new DuePlanItem(
                        plan.Id, plan.Name, asset.Id, asset.AssetNumber, asset.Name,
                        plan.Frequency ?? "", last, dueOn));
                }
            }
        }

        return due.OrderBy(d => d.DueOn).ThenBy(d => d.AssetNumber).ToList();
    }

    /// <summary>Opens a preventive work order for that asset and plan. The asset goes Under maintenance.</summary>
    public async Task<int> OpenWorkOrderAsync(int planId, int assetId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var plan = await db.PreventiveMaintenancePlans.AsNoTracking()
                       .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive)
                   ?? throw new InvalidOperationException("Plan not found.");

        return await _workOrders.CreateAsync(
            new WorkOrderInput(
                assetId,
                MaintenanceType.Preventive,
                $"Preventive maintenance: {plan.Name}",
                TechnicianId: null,
                VendorId: null,
                DateTime.UtcNow.Date),
            incidentId: null,
            planId: planId);
    }
}
