namespace AMS.Web.Components.Pages.Transactions;

public partial class DisposalList
{
    [Inject] private DisposalService Disposals { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = default!;

    [SupplyParameterFromQuery] public int? AssetId { get; set; }

    private List<DisposalRequest>? _rows;
    private List<Asset> _assets = new();
    private Dictionary<int, string> _userNames = new();
    private bool _isApprover;
    private string _status = string.Empty;
    private int _total;
    private int _page = 1;


    private bool _open;
    private int _assetId;
    private DateTime _date = DateTime.Today;
    private decimal? _netBookValue;
    private decimal? _saleProceeds;
    private decimal? _gainLoss;
    private string? _reasonText;

    private int? _rejectId;
    private string _rejectReason = string.Empty;
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
                await OnAssetChangedAsync();
            }
            else
            {
                _error = "No disposal request can be made for that asset.";
            }
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Disposals.GetAllAsync(string.IsNullOrEmpty(_status) ? null : _status, _page);
            _rows = result.Items;
            _total = result.Total;
            _assets = await Disposals.GetDisposableAssetsAsync();
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
        _date = DateTime.Today;
        _netBookValue = null;
        _saleProceeds = null;
        _gainLoss = null;
        _reasonText = null;
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    private async Task OnAssetChangedAsync()
    {
        if (_assetId == 0) return;

        try
        {
            _netBookValue = await Disposals.SuggestNetBookValueAsync(_assetId, _date);
            Recalculate();
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    private void Recalculate()
    {
        _gainLoss = _saleProceeds is null && _netBookValue is null
            ? null
            : (_saleProceeds ?? 0m) - (_netBookValue ?? 0m);
    }

    private async Task SaveAsync()
    {
        _error = null;
        if (_assetId == 0) { _error = "Asset is required."; return; }
        if (string.IsNullOrWhiteSpace(_reasonText)) { _error = "A disposal reason is required."; return; }
        if (_date == default) { _error = "The date is required."; return; }

        try
        {
            await Disposals.CreateAsync(new DisposalInput(_assetId, _reasonText, _date, _saleProceeds, _netBookValue));
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
            await Disposals.ApproveAsync(id);
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private void OpenReject(int id)
    {
        _rejectId = id;
        _rejectReason = string.Empty;
        _error = null;
    }

    private void CloseReject() => _rejectId = null;

    private async Task RejectAsync()
    {
        if (_rejectId is null) return;
        if (string.IsNullOrWhiteSpace(_rejectReason)) { _error = "A rejection reason is required."; return; }

        try
        {
            await Disposals.RejectAsync(_rejectId.Value, _rejectReason);
            _rejectId = null;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }
}
