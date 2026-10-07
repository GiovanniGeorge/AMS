using Microsoft.EntityFrameworkCore;
using AMS.Application.Maintenance;
using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class MaintenancePlanService : IMaintenancePlanService
{
    private readonly ApplicationDbContext _db;
    private readonly IWorkOrderService _workOrders;

    public MaintenancePlanService(ApplicationDbContext db, IWorkOrderService workOrders)
    {
        _db = db;
        _workOrders = workOrders;
    }

    public async Task<List<PreventiveMaintenancePlan>> GetAllAsync()
    {
        return await _db.PreventiveMaintenancePlans
            .AsNoTracking()
            .Include(p => p.Asset)
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(MaintenancePlanInput input)
    {
        Validate(input);

        try
        {
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

            _db.PreventiveMaintenancePlans.Add(plan);
            await _db.SaveChangesAsync();
            return plan.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task UpdateAsync(int id, MaintenancePlanInput input)
    {
        Validate(input);

        try
        {
            var plan = await _db.PreventiveMaintenancePlans.FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
                       ?? throw new InvalidOperationException("Plan not found.");

            plan.Name = input.Name.Trim();
            plan.AssetId = input.AssetId;
            plan.CategoryId = input.CategoryId;
            plan.Frequency = input.Frequency;
            plan.RepeatSchedule = input.ScheduleNotes;
            plan.UsageThreshold = input.UsageThreshold;
            plan.StartDate = input.StartDate;
            plan.EndDate = input.EndDate;

            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
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

    public async Task<List<DuePlanItem>> GetDueAsync()
    {
        var today = DateTime.UtcNow.Date;

        var plans = await _db.PreventiveMaintenancePlans
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

            var assets = await _db.Assets
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
                var last = await _db.WorkOrders
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

    public async Task<int> OpenWorkOrderAsync(int planId, int assetId)
    {
        var plan = await _db.PreventiveMaintenancePlans.AsNoTracking()
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
