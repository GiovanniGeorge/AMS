namespace AMS.Web.Components.Pages.Maintenance;

public partial class ContractList
{
    [Inject] private CrudService<MaintenanceContract> Contracts { get; set; } = default!;
    [Inject] private CrudService<Vendor> Vendors { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;

    private List<MaintenanceContract>? _rows;
    private List<Vendor> _vendors = new();
    private Dictionary<int, string> _userNames = new();
    private bool _showInactive;

    private bool _open;
    private int? _editId;
    private string? _number;
    private int? _vendorId;
    private string? _coverage;
    private DateTime? _startDate = DateTime.Today;
    private DateTime? _endDate;
    private string? _error;

    private int Expiring => _rows?.Count(c => c.IsActive && IsExpiring(c)) ?? 0;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _vendors = await Vendors.GetAllAsync(true);
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
        }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _rows = await Contracts.GetAllAsync(_showInactive);
            _error = null;
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    private void OpenNew()
    {
        _editId = null;
        _number = null;
        _vendorId = null;
        _coverage = null;
        _startDate = DateTime.Today;
        _endDate = null;
        _error = null;
        _open = true;
    }

    private void OpenEdit(MaintenanceContract contract)
    {
        _editId = contract.Id;
        _number = contract.ContractNumber;
        _vendorId = contract.VendorId;
        _coverage = contract.CoverageDetails;
        _startDate = contract.StartDate;
        _endDate = contract.EndDate;
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    private async Task SaveAsync()
    {
        _error = null;
        if (string.IsNullOrWhiteSpace(_number)) { _error = "The contract number is required."; return; }
        if (_startDate is not null && _endDate is not null && _endDate < _startDate)
        {
            _error = "The end date cannot be before the start date.";
            return;
        }

        try
        {
            if (_editId is int id)
            {
                var existing = await Contracts.GetByIdAsync(id);
                if (existing is null) { _error = "Contract not found."; return; }

                existing.ContractNumber = _number!.Trim();
                existing.VendorId = _vendorId;
                existing.CoverageDetails = _coverage;
                existing.StartDate = _startDate;
                existing.EndDate = _endDate;

                await Contracts.UpdateAsync(existing);
            }
            else
            {
                await Contracts.AddAsync(new MaintenanceContract
                {
                    ContractNumber = _number!.Trim(),
                    VendorId = _vendorId,
                    CoverageDetails = _coverage,
                    StartDate = _startDate,
                    EndDate = _endDate
                });
            }

            _open = false;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task ToggleAsync(MaintenanceContract contract)
    {
        if (contract.IsActive) await Contracts.DeactivateAsync(contract.Id);
        else await Contracts.ActivateAsync(contract.Id);
        await LoadAsync();
    }

    private static bool IsExpired(MaintenanceContract c) =>
        c.EndDate is DateTime end && end.Date < DateTime.Today;

    private static bool IsExpiring(MaintenanceContract c) =>
        c.EndDate is DateTime end && end.Date >= DateTime.Today && end.Date <= DateTime.Today.AddDays(30);

    private async Task QuickVendorAddedAsync(Vendor created)
    {
        _vendors = await Vendors.GetAllAsync();
        _vendorId = created.Id;
    }
}
