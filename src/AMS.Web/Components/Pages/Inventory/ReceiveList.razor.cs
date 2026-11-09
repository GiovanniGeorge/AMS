namespace AMS.Web.Components.Pages.Inventory;

public partial class ReceiveList
{
    [Inject] private ReceiveService Receives { get; set; } = default!;
    [Inject] private PurchaseOrderService PurchaseOrders { get; set; } = default!;
    [Inject] private CrudService<Store> Stores { get; set; } = default!;
    [Inject] private CrudService<Supplier> Suppliers { get; set; } = default!;
    [Inject] private CrudService<Item> Items { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;

    private List<Receive>? _rows;
    private List<Store> _stores = new();
    private List<Supplier> _suppliers = new();
    private List<Item> _items = new();
    private List<Asset> _assets = new();
    private List<PurchaseOrder> _orders = new();
    private Dictionary<int, string> _userNames = new();

    private bool _open;
    private int _storeId;
    private int? _supplierId;
    private int? _purchaseOrderId;
    private DateTime _date = DateTime.Today;
    private string? _notes;
    private List<ReceiveLineInput> _lines = new();

    private string _lineKind = "item";
    private int _lineItemId;
    private int _lineAssetId;
    private decimal _lineQuantity = 1;

    private string? _error;
    private int _total;
    private int _page = 1;


    protected override async Task OnInitializedAsync()
    {
        try
        {
            _stores = await Stores.GetAllAsync();
            _suppliers = await Suppliers.GetAllAsync();
            _items = await Items.GetAllAsync();
            _orders = await PurchaseOrders.GetOpenAsync();
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
        }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Receives.GetAllAsync(_page);
            _rows = result.Items;
            _total = result.Total;
            _assets = await Receives.GetReceivableAssetsAsync();
            _error = null;
        }
        catch (Exception ex) { _error = ex.Message; }
    }


    private async Task GoToAsync(int page)
    {
        _page = page;
        await LoadAsync();
    }

    private async Task FilterAsync()
    {
        _page = 1;
        await LoadAsync();
    }

    private void OpenNew()
    {
        _storeId = _stores.Count == 1 ? _stores[0].Id : 0;
        _supplierId = null;
        _purchaseOrderId = null;
        _date = DateTime.Today;
        _notes = null;
        _lines = new();
        _lineKind = "item";
        _lineItemId = 0;
        _lineAssetId = 0;
        _lineQuantity = 1;
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    private void AddLine()
    {
        _error = null;

        if (_lineKind == "item")
        {
            if (_lineItemId == 0) { _error = "Choose an item."; return; }
            if (_lineQuantity <= 0) { _error = "The quantity must be greater than zero."; return; }
            _lines.Add(new ReceiveLineInput(_lineItemId, null, _lineQuantity));
            _lineItemId = 0;
            _lineQuantity = 1;
        }
        else
        {
            if (_lineAssetId == 0) { _error = "Choose an asset."; return; }
            if (_lines.Any(l => l.AssetId == _lineAssetId)) { _error = "That asset is already on this delivery."; return; }
            _lines.Add(new ReceiveLineInput(null, _lineAssetId, 1));
            _lineAssetId = 0;
        }
    }

    private string LineText(ReceiveLineInput line)
    {
        if (line.AssetId is int assetId)
        {
            var asset = _assets.FirstOrDefault(a => a.Id == assetId);
            return asset is null ? "Asset" : $"{asset.AssetNumber} — {asset.Name}";
        }

        return _items.FirstOrDefault(i => i.Id == line.ItemId)?.Name ?? "Item";
    }

    private async Task SaveAsync()
    {
        _error = null;
        if (_storeId == 0) { _error = "Store is required."; return; }
        if (_date == default) { _error = "The date is required."; return; }
        if (_lines.Count == 0) { _error = "Add at least one line."; return; }

        try
        {
            await Receives.CreateAsync(new ReceiveInput(_date, _storeId, _supplierId, _purchaseOrderId, _notes, _lines));
            _open = false;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task QuickStoreAddedAsync(Store created)
    {
        _stores = await Stores.GetAllAsync();
        _storeId = created.Id;
    }

    private async Task QuickSupplierAddedAsync(Supplier created)
    {
        _suppliers = await Suppliers.GetAllAsync();
        _supplierId = created.Id;
    }

    private async Task QuickItemAddedAsync(Item created)
    {
        _items = await Items.GetAllAsync();
        _lineItemId = created.Id;
    }
}
