using Microsoft.EntityFrameworkCore;
using AMS.Application.Transactions;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;
using AMS.Application.Common;

namespace AMS.Infrastructure.Services;

/// <summary>
/// An asset coming back from an employee into a store. There is no approval:
/// the asset becomes In store, its custodian is cleared and the open custody is closed
/// with the return date, all at once.
/// </summary>
public class ReturnService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public ReturnService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<Paged<Return>> GetAllAsync(int page = 1)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = db.Returns
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

    /// <summary>Assets that can be returned: active and Assigned.</summary>
    public async Task<List<Asset>> GetReturnableAssetsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Assets
            .AsNoTracking()
            .Include(a => a.Custodian)
            .Where(a => a.IsActive && a.LifecycleStage == LifecycleStage.Assigned)
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(ReturnInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == input.AssetId)
                    ?? throw new InvalidOperationException("Asset not found.");

        if (!asset.IsActive || asset.LifecycleStage != LifecycleStage.Assigned)
        {
            throw new InvalidOperationException("Only an asset that is Assigned can be returned.");
        }

        var store = await db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == input.StoreId && s.IsActive)
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
        db.Returns.Add(entry);

        // Close the open custody period, if any.
        var openCustody = await db.AssetAssignments
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

        await db.SaveChangesAsync();
        return entry.Id;
    }
}
