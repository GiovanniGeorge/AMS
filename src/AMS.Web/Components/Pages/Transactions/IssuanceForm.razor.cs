namespace AMS.Web.Components.Pages.Transactions;

public partial class IssuanceForm
{
    [Inject] private AssetIssuanceService Issuance { get; set; } = default!;
    [Inject] private CrudService<Store> Stores { get; set; } = default!;
    [Inject] private CrudService<Employee> Employees { get; set; } = default!;
    [Inject] private CrudService<Department> Departments { get; set; } = default!;
    [Inject] private CrudService<Location> Locations { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [SupplyParameterFromQuery] public int? AssetId { get; set; }

    private List<Asset> _assets = new();
    private List<Store> _stores = new();
    private List<Employee> _employees = new();
    private List<Department> _departments = new();
    private List<Location> _locations = new();

    private int _assetId;
    private int _storeId;
    private int _employeeId;
    private int? _departmentId;
    private int? _locationId;
    private DateTime _date = DateTime.Today;
    private string? _notes;

    private bool _loading = true;
    private bool _saving;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _assets = await Issuance.GetIssuableAssetsAsync();
            _stores = await Stores.GetAllAsync();
            _employees = await Employees.GetAllAsync();
            _departments = await Departments.GetAllAsync();
            _locations = await Locations.GetAllAsync();

            // Opened from an asset's page with ?assetId=...
            if (AssetId is int id)
            {
                if (_assets.Any(a => a.Id == id)) _assetId = id;
                else _error = "That asset cannot be issued: it is not In store, or it already has a pending request.";
            }
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _loading = false; }
    }

    private void OnEmployeeChanged()
    {
        _departmentId = _employees.FirstOrDefault(e => e.Id == _employeeId)?.DepartmentId;
    }

    private async Task SubmitAsync()
    {
        _error = null;
        if (_assetId == 0) { _error = "Asset is required."; return; }
        if (_storeId == 0) { _error = "Store is required."; return; }
        if (_employeeId == 0) { _error = "Employee is required."; return; }
        if (_date == default) { _error = "The date is required."; return; }

        _saving = true;
        try
        {
            await Issuance.CreateAsync(new IssuanceRequest(
                _assetId, _storeId, _employeeId, _departmentId, _locationId, _date, _notes));
            Nav.NavigateTo("issuance");
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
        finally { _saving = false; }
    }

    private async Task QuickStoreAddedAsync(Store created)
    {
        _stores = await Stores.GetAllAsync();
        _storeId = created.Id;
    }

    private async Task QuickEmployeeAddedAsync(Employee created)
    {
        _employees = await Employees.GetAllAsync();
        _employeeId = created.Id;
    }

    private async Task QuickDepartmentAddedAsync(Department created)
    {
        _departments = await Departments.GetAllAsync();
        _departmentId = created.Id;
    }

    private async Task QuickLocationAddedAsync(Location created)
    {
        _locations = await Locations.GetAllAsync();
        _locationId = created.Id;
    }
}
