namespace AMS.Web.Components.Shared;

public partial class AssignAssetModal
{
    [Inject] private AssetIssuanceService Issuance { get; set; } = default!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = default!;
    [Inject] private CrudService<Employee> Employees { get; set; } = default!;
    [Inject] private CrudService<Store> Stores { get; set; } = default!;
    [Inject] private CrudService<Location> Locations { get; set; } = default!;

    [Parameter] public bool Visible { get; set; }

    /// <summary>Fixed employee (the employee screen). Null means pick one here.</summary>
    [Parameter] public int? EmployeeId { get; set; }

    /// <summary>Fixed asset (the asset screen). Null means pick one here.</summary>
    [Parameter] public int? AssetId { get; set; }

    [Parameter] public string? EmployeeLabel { get; set; }
    [Parameter] public string? AssetLabel { get; set; }

    /// <summary>Raised after a successful save, with true when the asset actually moved.</summary>
    [Parameter] public EventCallback<bool> OnAssigned { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }

    private List<Asset> _assets = new();
    private List<Employee> _employees = new();
    private List<Store> _stores = new();
    private List<Location> _locations = new();

    private int _assetId;
    private int _employeeId;
    private int _storeId;
    private int? _locationId;
    private DateTime _date = DateTime.Today;
    private string? _notes;

    private bool _isAdmin;
    private bool _loading;
    private bool _loaded;
    private string? _error;

    private string Title => _isAdmin ? "Assign Asset" : "Request Asset Assignment";

    // The lists are only worth fetching once the modal is actually opened.
    protected override async Task OnParametersSetAsync()
    {
        if (!Visible)
        {
            _loaded = false;
            return;
        }

        if (_loaded) return;
        _loaded = true;
        _loading = true;
        _error = null;

        try
        {
            _isAdmin = await CurrentUser.IsInRoleAsync(AppRoles.Admin);

            if (AssetId is null) _assets = await Issuance.GetIssuableAssetsAsync();
            if (EmployeeId is null) _employees = await Employees.GetAllAsync();
            _stores = await Stores.GetAllAsync();
            _locations = await Locations.GetAllAsync();

            _assetId = AssetId ?? 0;
            _employeeId = EmployeeId ?? 0;
            _storeId = _stores.Count == 1 ? _stores[0].Id : 0;
            _locationId = null;
            _date = DateTime.Today;
            _notes = null;
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _loading = false; }
    }

    private async Task SaveAsync()
    {
        _error = null;

        var assetId = AssetId ?? _assetId;
        var employeeId = EmployeeId ?? _employeeId;

        if (assetId == 0) { _error = "Choose an asset."; return; }
        if (employeeId == 0) { _error = "Choose an employee."; return; }
        if (_storeId == 0) { _error = "Choose the store the asset leaves from."; return; }
        if (_date == default) { _error = "Choose a date."; return; }

        var request = new IssuanceRequest(assetId, _storeId, employeeId, null, _locationId, _date, _notes);

        try
        {
            if (_isAdmin) await Issuance.CreateApprovedAsync(request);
            else await Issuance.CreateAsync(request);

            _loaded = false;
            await OnAssigned.InvokeAsync(_isAdmin);
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task CancelAsync()
    {
        _loaded = false;
        _error = null;
        await OnCancel.InvokeAsync();
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

    private async Task QuickLocationAddedAsync(Location created)
    {
        _locations = await Locations.GetAllAsync();
        _locationId = created.Id;
    }
}
