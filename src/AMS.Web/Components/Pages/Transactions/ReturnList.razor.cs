namespace AMS.Web.Components.Pages.Transactions;

public partial class ReturnList
{
    [Inject] private ReturnService Returns { get; set; } = default!;
    [Inject] private CrudService<Employee> Employees { get; set; } = default!;
    [Inject] private CrudService<Store> Stores { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;

    [SupplyParameterFromQuery] public int? AssetId { get; set; }

    private List<Return>? _rows;
    private List<Asset> _assets = new();
    private List<Employee> _employees = new();
    private List<Store> _stores = new();
    private Dictionary<int, string> _userNames = new();

    private bool _open;
    private int _assetId;
    private int _employeeId;
    private int _storeId;
    private DateTime _date = DateTime.Today;
    private string? _condition;
    private string? _reason;
    private string? _error;
    private int _total;
    private int _page = 1;


    protected override async Task OnInitializedAsync()
    {
        try
        {
            _employees = await Employees.GetAllAsync();
            _stores = await Stores.GetAllAsync();
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
        }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();

        // Opened from an asset's page with ?assetId=...
        if (AssetId is int id)
        {
            OpenNew();
            if (_assets.Any(a => a.Id == id))
            {
                _assetId = id;
                OnAssetChanged();
            }
            else
            {
                _error = "That asset cannot be returned: it is not Assigned.";
            }
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Returns.GetAllAsync(_page);
            _rows = result.Items;
            _total = result.Total;
            _assets = await Returns.GetReturnableAssetsAsync();
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
        _assetId = 0;
        _employeeId = 0;
        _storeId = _stores.Count == 1 ? _stores[0].Id : 0;
        _date = DateTime.Today;
        _condition = null;
        _reason = null;
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    private void OnAssetChanged()
    {
        var asset = _assets.FirstOrDefault(a => a.Id == _assetId);
        _employeeId = asset?.CustodianId ?? 0;
    }

    private async Task SaveAsync()
    {
        _error = null;
        if (_assetId == 0) { _error = "Asset is required."; return; }
        if (_employeeId == 0) { _error = "The employee returning the asset is required."; return; }
        if (_storeId == 0) { _error = "Store is required."; return; }
        if (_date == default) { _error = "The date is required."; return; }

        try
        {
            await Returns.CreateAsync(new ReturnInput(_assetId, _employeeId, _storeId, _date, _condition, _reason));
            _open = false;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task QuickEmployeeAddedAsync(Employee created)
    {
        _employees = await Employees.GetAllAsync();
        _employeeId = created.Id;
    }

    private async Task QuickStoreAddedAsync(Store created)
    {
        _stores = await Stores.GetAllAsync();
        _storeId = created.Id;
    }
}
