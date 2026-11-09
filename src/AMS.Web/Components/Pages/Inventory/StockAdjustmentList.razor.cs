namespace AMS.Web.Components.Pages.Inventory;

public partial class StockAdjustmentList
{
    [Inject] private StockService Stock { get; set; } = default!;
    [Inject] private CrudService<Store> Stores { get; set; } = default!;
    [Inject] private CrudService<Item> Items { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;

    private List<StockAdjustment>? _rows;
    private List<Store> _stores = new();
    private List<Item> _items = new();
    private Dictionary<int, string> _userNames = new();

    private bool _open;
    private int _storeId;
    private int _itemId;
    private decimal _current;
    private string _direction = StockDirection.Decrease;
    private decimal _quantity = 1;
    private DateTime _date = DateTime.Today;
    private string? _reason;
    private string? _error;
    private int _total;
    private int _page = 1;


    protected override async Task OnInitializedAsync()
    {
        try
        {
            _stores = await Stores.GetAllAsync();
            _items = await Items.GetAllAsync();
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
        }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Stock.GetAdjustmentsAsync(_page);
            _rows = result.Items;
            _total = result.Total;
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
        _itemId = 0;
        _current = 0;
        _direction = StockDirection.Decrease;
        _quantity = 1;
        _date = DateTime.Today;
        _reason = null;
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    private async Task ShowBalanceAsync()
    {
        _current = _storeId == 0 || _itemId == 0 ? 0 : await Stock.GetQuantityAsync(_storeId, _itemId);
    }

    private async Task SaveAsync()
    {
        _error = null;
        if (_storeId == 0) { _error = "Store is required."; return; }
        if (_itemId == 0) { _error = "Item is required."; return; }
        if (_quantity <= 0) { _error = "The quantity must be greater than zero."; return; }
        if (string.IsNullOrWhiteSpace(_reason)) { _error = "A reason is required."; return; }
        if (_date == default) { _error = "The date is required."; return; }

        try
        {
            await Stock.AdjustAsync(new StockAdjustmentInput(_storeId, _itemId, _quantity, _direction, _reason!, _date));
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

    private async Task QuickItemAddedAsync(Item created)
    {
        _items = await Items.GetAllAsync();
        _itemId = created.Id;
    }
}
