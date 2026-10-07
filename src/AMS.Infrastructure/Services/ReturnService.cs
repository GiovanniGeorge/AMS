using Microsoft.EntityFrameworkCore;
using AMS.Application.Transactions;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;
using AMS.Application.Common;

namespace AMS.Infrastructure.Services;

public class ReturnService : IReturnService
{
    private readonly ApplicationDbContext _db;

    public ReturnService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Paged<Return>> GetAllAsync(int page = 1)
    {
        var query = _db.Returns
            .AsNoTracking()
            .Include(r => r.Asset)
            .Include(r => r.Employee)
            .Include(r => r.Store)
            .Where(r => r.IsActive && r.AssetId != null);

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(r => r.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<Return>.Size)
            .Take(Paged<Return>.Size)
            .ToListAsync();

        return new Paged<Return>(items, total);
    }

    public async Task<List<Asset>> GetReturnableAssetsAsync()
    {
        return await _db.Assets
            .AsNoTracking()
            .Include(a => a.Custodian)
            .Where(a => a.IsActive && a.LifecycleStage == LifecycleStage.Assigned)
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(ReturnInput input)
    {
        try
        {
            var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == input.AssetId)
                        ?? throw new InvalidOperationException("Asset not found.");

            if (!asset.IsActive || asset.LifecycleStage != LifecycleStage.Assigned)
            {
                throw new InvalidOperationException("Only an asset that is Assigned can be returned.");
            }

            var store = await _db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == input.StoreId && s.IsActive)
                        ?? throw new InvalidOperationException("Store not found.");

            var entry = new Return
            {
                Date = input.Date,
                AssetId = input.AssetId,
                EmployeeId = input.EmployeeId,
                StoreId = input.StoreId,
                Condition = input.Condition,
                Reason = input.Reason,
                Status = "Completed"
            };
            _db.Returns.Add(entry);

            // Close the open custody period, if any.
            var openCustody = await _db.AssetAssignments
                .Where(a => a.IsActive
                            && a.AssetId == input.AssetId
                            && a.ReturnDate == null
                            && a.Status == RequestStatus.Approved)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();

            if (openCustody is not null)
            {
                openCustody.ReturnDate = input.Date;
            }

            // The asset is back in the store: no custodian, no department, the store's location.
            asset.CustodianId = null;
            asset.DepartmentId = null;
            asset.LocationId = store.LocationId ?? asset.LocationId;
            asset.LifecycleStage = LifecycleStage.InStore;

            await _db.SaveChangesAsync();
            return entry.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }
}
