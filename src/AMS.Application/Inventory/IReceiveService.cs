using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Application.Common;

namespace AMS.Application.Inventory;

/// <summary>One line of a delivery: either a consumable item with a quantity, or one asset.</summary>
public record ReceiveLineInput(int? ItemId, int? AssetId, decimal Quantity);

/// <summary>What the user fills in on the "Receive" screen.</summary>
public record ReceiveInput(
    DateTime Date,
    int StoreId,
    int? SupplierId,
    int? PurchaseOrderId,
    string? Notes,
    List<ReceiveLineInput> Lines);

/// <summary>
/// Goods arriving into a store. There is no approval: it is applied at once.
/// An item line raises the store's stock balance; an asset line puts the asset In store
/// at that store's location.
/// </summary>
public interface IReceiveService
{
    Task<Paged<Receive>> GetAllAsync(int page = 1);

    /// <summary>Assets a delivery can contain: active and not disposed of.</summary>
    Task<List<Asset>> GetReceivableAssetsAsync();

    Task<int> CreateAsync(ReceiveInput input);
}
