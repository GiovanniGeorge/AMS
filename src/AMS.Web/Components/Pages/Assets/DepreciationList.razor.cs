namespace AMS.Web.Components.Pages.Assets;

public partial class DepreciationList
{
    [Inject] private DepreciationService Depreciation { get; set; } = default!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = default!;

    private List<DepreciationEntry>? _rows;
    private List<DepreciationPreviewLine>? _preview;
    private bool _isAdmin;

    private string _period = DepreciationPeriod.Monthly;
    private DateTime _periodEnd = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
    private bool _busy;
    private int _total;
    private int _page = 1;

    private string? _error;
    private string? _posted;

    private int Postable => _preview?.Count(l => l.Skipped is null) ?? 0;

    protected override async Task OnInitializedAsync()
    {
        try { _isAdmin = await CurrentUser.IsInRoleAsync(AppRoles.Admin); }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Depreciation.GetAllAsync(_page);
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

    private void ClearPreview()
    {
        _preview = null;
        _posted = null;
    }

    private async Task PreviewAsync()
    {
        _error = null;
        _posted = null;
        _busy = true;

        try { _preview = await Depreciation.PreviewAsync(_period, _periodEnd); }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
        finally { _busy = false; }
    }

    private async Task PostAsync()
    {
        _error = null;
        _busy = true;

        try
        {
            var count = await Depreciation.PostAsync(_period, _periodEnd);
            _posted = $"{count} entry(ies) posted for {_periodEnd:yyyy-MM-dd}.";
            _preview = null;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
        finally { _busy = false; }
    }
}
