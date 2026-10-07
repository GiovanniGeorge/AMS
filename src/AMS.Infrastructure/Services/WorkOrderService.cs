using Microsoft.EntityFrameworkCore;
using AMS.Application.Maintenance;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;
using AMS.Application.Common;

namespace AMS.Infrastructure.Services;

public class WorkOrderService : IWorkOrderService
{
    private readonly ApplicationDbContext _db;

    public WorkOrderService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Paged<WorkOrder>> GetAllAsync(string? status = null, int page = 1)
    {
        var query = _db.WorkOrders
            .AsNoTracking()
            .Include(w => w.Asset)
            .Include(w => w.Technician)
            .Include(w => w.Vendor)
            .Include(w => w.Incident)
            .Include(w => w.PreventiveMaintenancePlan)
            .Where(w => w.IsActive);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(w => w.Status == status);
        }

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(w => w.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<WorkOrder>.Size)
            .Take(Paged<WorkOrder>.Size)
            .ToListAsync();

        return new Paged<WorkOrder>(items, total);
    }

    public async Task<List<Asset>> GetMaintainableAssetsAsync()
    {
        return await _db.Assets
            .AsNoTracking()
            .Where(a => a.IsActive
                        && a.LifecycleStage != LifecycleStage.Disposed
                        && a.LifecycleStage != LifecycleStage.UnderMaintenance
                        && a.LifecycleStage != LifecycleStage.InTransit)
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(WorkOrderInput input, int? incidentId = null, int? planId = null)
    {
        try
        {
            var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == input.AssetId)
                        ?? throw new InvalidOperationException("Asset not found.");

            if (!asset.IsActive || asset.LifecycleStage == LifecycleStage.Disposed)
            {
                throw new InvalidOperationException("A disposed or inactive asset cannot go into maintenance.");
            }

            if (asset.LifecycleStage == LifecycleStage.InTransit)
            {
                throw new InvalidOperationException("The asset is in transit; confirm its receipt first.");
            }

            var workOrder = new WorkOrder
            {
                AssetId = input.AssetId,
                MaintenanceType = input.MaintenanceType,
                Problem = input.Problem,
                TechnicianId = input.TechnicianId,
                VendorId = input.VendorId,
                StartDate = input.StartDate,
                Status = WorkOrderStatus.Open,
                IncidentId = incidentId,
                PreventiveMaintenancePlanId = planId
            };

            _db.WorkOrders.Add(workOrder);
            asset.LifecycleStage = LifecycleStage.UnderMaintenance;

            await _db.SaveChangesAsync();
            return workOrder.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task CloseAsync(WorkOrderClosure closure)
    {
        try
        {
            var workOrder = await _db.WorkOrders
                .FirstOrDefaultAsync(w => w.Id == closure.WorkOrderId && w.IsActive)
                ?? throw new InvalidOperationException("Work order not found.");

            if (workOrder.Status == WorkOrderStatus.Closed)
            {
                throw new InvalidOperationException("This work order is already closed.");
            }

            workOrder.WorkDone = closure.WorkDone;
            workOrder.PartsUsed = closure.PartsUsed;
            workOrder.Cost = closure.Cost;
            workOrder.Downtime = closure.Downtime;
            workOrder.EndDate = closure.EndDate;
            workOrder.Status = WorkOrderStatus.Closed;

            // The asset comes back from the workshop into the store, with no custodian.
            if (workOrder.AssetId is int assetId)
            {
                var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == assetId);
                if (asset is not null && asset.LifecycleStage == LifecycleStage.UnderMaintenance)
                {
                    var openCustody = await _db.AssetAssignments
                        .Where(a => a.IsActive && a.AssetId == assetId && a.ReturnDate == null
                                    && a.Status == RequestStatus.Approved)
                        .OrderByDescending(a => a.Id)
                        .FirstOrDefaultAsync();

                    if (openCustody is not null)
                    {
                        openCustody.ReturnDate = closure.EndDate;
                    }

                    asset.CustodianId = null;
                    asset.DepartmentId = null;
                    asset.LifecycleStage = LifecycleStage.InStore;
                }
            }

            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }
}
