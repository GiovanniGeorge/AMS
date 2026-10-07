using Microsoft.EntityFrameworkCore;
using AMS.Application.Inventory;
using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;
using AMS.Application.Common;

namespace AMS.Infrastructure.Services;

public class ReceiveService : IReceiveService
{
    private readonly ApplicationDbContext _db;

    public ReceiveService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Paged<Receive>> GetAllAsync(int page = 1)
    {
        var query = _db.Receives
            .AsNoTracking()
            .Include(r => r.Supplier)
            .Include(r => r.Store)
            .Include(r => r.PurchaseOrder)
            .Include(r => r.Lines.Where(l => l.IsActive))
                .ThenInclude(l => l.Item)
            .Include(r => r.Lines.Where(l => l.IsActive))
                .ThenInclude(l => l.Asset)
            .Where(r => r.IsActive);

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(r => r.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<Receive>.Size)
            .Take(Paged<Receive>.Size)
            .ToListAsync();

        return new Paged<Receive>(items, total);
    }

    public async Task<List<Asset>> GetReceivableAssetsAsync()
    {
        return await _db.Assets
            .AsNoTracking()
            .Where(a => a.IsActive && a.LifecycleStage != LifecycleStage.Disposed)
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(ReceiveInput input)
    {
        if (input.Lines.Count == 0)
        {
            throw new InvalidOperationException("A delivery needs at least one line.");
        }

        foreach (var line in input.Lines)
        {
            // CK_ReceiveLine_AssetOrItem in the database allows exactly one of the two.
            if ((line.ItemId is null) == (line.AssetId is null))
            {
                throw new InvalidOperationException("Every line must be either an item or an asset, not both.");
            }

            if (line.ItemId is not null && line.Quantity <= 0)
            {
                throw new InvalidOperationException("An item line needs a quantity greater than zero.");
            }
        }

        try
        {
            var store = await _db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == input.StoreId && s.IsActive)
                        ?? throw new InvalidOperationException("Store not found.");

            var receive = new Receive
            {
                Date = input.Date,
                StoreId = input.StoreId,
                SupplierId = input.SupplierId,
                PurchaseOrderId = input.PurchaseOrderId,
                Notes = input.Notes,
                Status = "Received"
            };

            foreach (var line in input.Lines)
            {
                receive.Lines.Add(new ReceiveLine
                {
                    ItemId = line.ItemId,
                    AssetId = line.AssetId,
                    Quantity = line.ItemId is null ? 1 : line.Quantity
                });

                if (line.ItemId is int itemId)
                {
                    // Consumable item: the store's balance goes up.
                    var balance = await _db.StockBalances
                        .FirstOrDefaultAsync(b => b.StoreId == input.StoreId && b.ItemId == itemId);

                    if (balance is null)
                    {
                        _db.StockBalances.Add(new StockBalance
                        {
                            StoreId = input.StoreId,
                            ItemId = itemId,
                            Quantity = line.Quantity
                        });
                    }
                    else
                    {
                        balance.Quantity += line.Quantity;
                        balance.IsActive = true;
                    }
                }
                else if (line.AssetId is int assetId)
                {
                    // Asset: it is now in the store.
                    var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == assetId)
                                ?? throw new InvalidOperationException("Asset not found.");

                    if (!asset.IsActive || asset.LifecycleStage == LifecycleStage.Disposed)
                    {
                        throw new InvalidOperationException($"Asset {asset.AssetNumber} cannot be received.");
                    }

                    asset.LocationId = store.LocationId ?? asset.LocationId;
                    asset.LifecycleStage = LifecycleStage.InStore;
                }
            }

            _db.Receives.Add(receive);
            await _db.SaveChangesAsync();
            return receive.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }
}
