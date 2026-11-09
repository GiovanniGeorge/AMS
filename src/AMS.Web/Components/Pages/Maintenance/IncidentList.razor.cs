namespace AMS.Web.Components.Pages.Maintenance;

public partial class IncidentList
{
    [Inject] private IncidentService Incidents { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = default!;

    [SupplyParameterFromQuery] public int? AssetId { get; set; }

    private List<Incident>? _rows;
    private List<Asset> _assets = new();
    private Dictionary<int, string> _userNames = new();
    private bool _isApprover;
    private int _total;
    private int _page = 1;

    private bool _open;
    private int _assetId;
    private string _custodian = "—";
    private IncidentType _type = IncidentType.Damaged;
    private DateTime _date = DateTime.Today;
    private string? _description;

    private int? _rejectId;
    private string _reason = string.Empty;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
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
                _error = "No incident can be reported on that asset.";
            }
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Incidents.GetAllAsync(null, _page);
            _rows = result.Items;
            _total = result.Total;
            _assets = await Incidents.GetReportableAssetsAsync();
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
        _custodian = "—";
        _type = IncidentType.Damaged;
        _date = DateTime.Today;
        _description = null;
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    private void OnAssetChanged()
    {
        var asset = _assets.FirstOrDefault(a => a.Id == _assetId);
        _custodian = asset?.Custodian?.Name ?? "Store";
    }

    private async Task SaveAsync()
    {
        _error = null;
        if (_assetId == 0) { _error = "Asset is required."; return; }
        if (_date == default) { _error = "The date is required."; return; }

        try
        {
            await Incidents.CreateAsync(new IncidentInput(_assetId, _type, _description, _date));
            _open = false;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task ToWorkOrderAsync(int id)
    {
        _error = null;
        try
        {
            await Incidents.SendToWorkOrderAsync(id);
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task ToDisposalAsync(int id)
    {
        _error = null;
        try
        {
            await Incidents.SendToDisposalAsync(id);
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
            await Incidents.RejectAsync(_rejectId.Value, _reason);
            _rejectId = null;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

}
