namespace AMS.Web.Components.Pages.Transactions;

public partial class RevaluationList
{
    [Inject] private RevaluationService Revaluations { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;

    [SupplyParameterFromQuery] public int? AssetId { get; set; }

    private List<RevaluationEvent>? _rows;
    private List<Asset> _assets = new();
    private Dictionary<int, string> _userNames = new();

    private bool _open;
    private int _assetId;
    private decimal? _oldValue;
    private decimal? _newValue;
    private DateTime _date = DateTime.Today;
    private string? _reason;
    private string? _error;
    private int _total;
    private int _page = 1;


    protected override async Task OnInitializedAsync()
    {
        try { _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username); }
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
                _error = "That asset cannot be revalued.";
            }
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Revaluations.GetAllAsync(_page);
            _rows = result.Items;
            _total = result.Total;
            _assets = await Revaluations.GetRevaluableAssetsAsync();
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
        _oldValue = null;
        _newValue = null;
        _date = DateTime.Today;
        _reason = null;
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    private async Task OnAssetChangedAsync()
    {
        if (_assetId == 0) { _oldValue = null; return; }

        try { _oldValue = await Revaluations.GetCurrentValueAsync(_assetId); }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task SaveAsync()
    {
        _error = null;
        if (_assetId == 0) { _error = "Asset is required."; return; }
        if (_newValue is null) { _error = "The new value is required."; return; }
        if (_date == default) { _error = "The date is required."; return; }

        try
        {
            await Revaluations.CreateAsync(new RevaluationInput(_assetId, _newValue.Value, _date, _reason));
            _open = false;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }
}
