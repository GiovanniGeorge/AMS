namespace AMS.Web.Components.Pages.Transactions;

public partial class TransferList
{
    [Inject] private TransferService Transfers { get; set; } = default!;
    [Inject] private CrudService<Employee> Employees { get; set; } = default!;
    [Inject] private CrudService<Department> Departments { get; set; } = default!;
    [Inject] private CrudService<Location> Locations { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = default!;

    [SupplyParameterFromQuery] public int? AssetId { get; set; }

    private List<Transfer>? _rows;
    private List<Asset> _assets = new();
    private List<Employee> _employees = new();
    private List<Department> _departments = new();
    private List<Location> _locations = new();
    private Dictionary<int, string> _userNames = new();

    private string _status = string.Empty;
    private bool _isApprover;
    private int _total;
    private int _page = 1;


    private bool _open;
    private int _assetId;
    private string _from = "—";
    private int? _toCustodianId;
    private int? _toDepartmentId;
    private int? _toLocationId;
    private DateTime _date = DateTime.Today;

    private int? _rejectId;
    private string _reason = string.Empty;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _employees = await Employees.GetAllAsync();
            _departments = await Departments.GetAllAsync();
            _locations = await Locations.GetAllAsync();
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
            _isApprover = await CurrentUser.IsInRoleAsync(AppRoles.Admin);
        }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();

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
                _error = "That asset cannot be transferred right now.";
            }
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Transfers.GetAllAsync(string.IsNullOrEmpty(_status) ? null : _status, _page);
            _rows = result.Items;
            _total = result.Total;
            _assets = await Transfers.GetTransferableAssetsAsync();
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
        _assetId = 0;
        _from = "—";
        _toCustodianId = null;
        _toDepartmentId = null;
        _toLocationId = null;
        _date = DateTime.Today;
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    private void OnAssetChanged()
    {
        var asset = _assets.FirstOrDefault(a => a.Id == _assetId);
        _from = asset is null ? "—" : Place(asset.Custodian?.Name, null, asset.Location?.Name);
    }

    private void OnCustodianChanged()
    {
        if (_toCustodianId is int id)
        {
            _toDepartmentId ??= _employees.FirstOrDefault(e => e.Id == id)?.DepartmentId;
        }
    }

    private async Task SaveAsync()
    {
        _error = null;
        if (_assetId == 0) { _error = "Asset is required."; return; }
        if (_toCustodianId is null && _toDepartmentId is null && _toLocationId is null)
        {
            _error = "Give at least one destination: custodian, department or location.";
            return;
        }
        if (_date == default) { _error = "The date is required."; return; }

        try
        {
            await Transfers.CreateAsync(new TransferInput(_assetId, _toCustodianId, _toDepartmentId, _toLocationId, _date));
            _open = false;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task ApproveAsync(int id)
    {
        _error = null;
        try
        {
            await Transfers.ApproveAsync(id);
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task ConfirmAsync(int id)
    {
        _error = null;
        try
        {
            await Transfers.ConfirmReceiptAsync(id);
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private void OpenReject(int id)
    {
        _rejectId = id;
        _reason = string.Empty;
        _error = null;
    }

    private void CloseReject() => _rejectId = null;

    private async Task RejectAsync()
    {
        if (_rejectId is null) return;
        if (string.IsNullOrWhiteSpace(_reason)) { _error = "A rejection reason is required."; return; }

        try
        {
            await Transfers.RejectAsync(_rejectId.Value, _reason);
            _rejectId = null;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private static string Place(string? custodian, string? department, string? location)
    {
        var parts = new[] { custodian, department, location }.Where(p => !string.IsNullOrWhiteSpace(p));
        var text = string.Join(" · ", parts);
        return text.Length == 0 ? "Store" : text;
    }

    private async Task QuickEmployeeAddedAsync(Employee created)
    {
        _employees = await Employees.GetAllAsync();
        _toCustodianId = created.Id;
    }

    private async Task QuickDepartmentAddedAsync(Department created)
    {
        _departments = await Departments.GetAllAsync();
        _toDepartmentId = created.Id;
    }

    private async Task QuickLocationAddedAsync(Location created)
    {
        _locations = await Locations.GetAllAsync();
        _toLocationId = created.Id;
    }
}
