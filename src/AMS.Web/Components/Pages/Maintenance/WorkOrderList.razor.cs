namespace AMS.Web.Components.Pages.Maintenance;

public partial class WorkOrderList
{
    [Inject] private WorkOrderService WorkOrders { get; set; } = default!;
    [Inject] private CrudService<Employee> Employees { get; set; } = default!;
    [Inject] private CrudService<Vendor> Vendors { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;

    [SupplyParameterFromQuery] public int? AssetId { get; set; }

    private List<WorkOrder>? _rows;
    private List<Asset> _assets = new();
    private List<Employee> _employees = new();
    private List<Vendor> _vendors = new();
    private Dictionary<int, string> _userNames = new();

    private string _status = string.Empty;
    private int _total;
    private int _page = 1;


    private bool _open;
    private int _assetId;
    private MaintenanceType _type = MaintenanceType.Corrective;
    private int? _technicianId;
    private int? _vendorId;
    private DateTime _startDate = DateTime.Today;
    private string? _problem;

    private int? _closeId;
    private DateTime _endDate = DateTime.Today;
    private decimal? _cost;
    private decimal? _downtime;
    private string? _workDone;
    private string? _partsUsed;

    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _employees = await Employees.GetAllAsync();
            _vendors = await Vendors.GetAllAsync();
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
        }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();

        if (AssetId is int id)
        {
            OpenNew();
            if (_assets.Any(a => a.Id == id)) _assetId = id;
            else _error = "No work order can be opened for that asset right now.";
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await WorkOrders.GetAllAsync(string.IsNullOrEmpty(_status) ? null : _status, _page);
            _rows = result.Items;
            _total = result.Total;
            _assets = await WorkOrders.GetMaintainableAssetsAsync();
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
        _type = MaintenanceType.Corrective;
        _technicianId = null;
        _vendorId = null;
        _startDate = DateTime.Today;
        _problem = null;
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    private async Task SaveAsync()
    {
        _error = null;
        if (_assetId == 0) { _error = "Asset is required."; return; }
        if (_startDate == default) { _error = "The start date is required."; return; }

        try
        {
            await WorkOrders.CreateAsync(new WorkOrderInput(_assetId, _type, _problem, _technicianId, _vendorId, _startDate));
            _open = false;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private void OpenClose(int id)
    {
        _closeId = id;
        _endDate = DateTime.Today;
        _cost = null;
        _downtime = null;
        _workDone = null;
        _partsUsed = null;
        _error = null;
    }

    private void CancelClose() => _closeId = null;

    private async Task CloseWorkOrderAsync()
    {
        if (_closeId is null) return;
        if (_endDate == default) { _error = "The end date is required."; return; }

        try
        {
            await WorkOrders.CloseAsync(new WorkOrderClosure(_closeId.Value, _workDone, _partsUsed, _cost, _downtime, _endDate));
            _closeId = null;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task QuickEmployeeAddedAsync(Employee created)
    {
        _employees = await Employees.GetAllAsync();
        _technicianId = created.Id;
    }

    private async Task QuickVendorAddedAsync(Vendor created)
    {
        _vendors = await Vendors.GetAllAsync();
        _vendorId = created.Id;
    }
}
