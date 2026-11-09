namespace AMS.Web.Components.Pages.Assets;

public partial class AssetList
{
    [Inject] private AssetService Assets { get; set; } = default!;
    [Inject] private CrudService<Asset> AssetCrud { get; set; } = default!;
    [Inject] private CrudService<Category> Categories { get; set; } = default!;
    [Inject] private CrudService<Location> Locations { get; set; } = default!;
    [Inject] private CrudService<AssetStatus> Statuses { get; set; } = default!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    // The dashboard tiles link here with ?stage=... and ?status=..., so one screen
    // serves both the register and every "show me these assets" shortcut.
    [Parameter, SupplyParameterFromQuery(Name = "stage")] public string? StageQuery { get; set; }
    [Parameter, SupplyParameterFromQuery(Name = "status")] public int? StatusQuery { get; set; }

    private List<Asset>? _rows;
    private List<Category> _categories = new();
    private List<Location> _locations = new();
    private List<AssetStatus> _statuses = new();
    private Dictionary<int, string> _userNames = new();

    private Asset? _assign;

    private string AssignLabel => _assign is null
        ? string.Empty
        : _assign.AssetNumber + " — " + _assign.Name;
    private string? _notice;

    private string? _search;
    private string _stage = string.Empty;
    private int? _statusId;
    private int? _categoryId;
    private int? _locationId;
    private bool _showInactive;
    private string? _error;

    private int _total;
    private int _page = 1;
    private const int _pageSize = 50;
    private string? _csv;
    private bool _building;

    private int Pages => _total == 0 ? 1 : (_total + _pageSize - 1) / _pageSize;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _categories = await Categories.GetAllAsync();
            _locations = await Locations.GetAllAsync();
            _statuses = await Statuses.GetAllAsync();
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
        }
        catch (Exception ex) { _error = ex.Message; }

        if (!string.IsNullOrWhiteSpace(StageQuery)) _stage = StageQuery;
        if (StatusQuery is { } fromUrl) _statusId = fromUrl;

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Assets.SearchAsync(Filter(_page, _pageSize));
            _rows = result.Items;
            _total = result.Total;
            _error = null;
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    private AssetFilter Filter(int page, int pageSize)
    {
        LifecycleStage? stage = Enum.TryParse<LifecycleStage>(_stage, out var s) ? s : null;
        return new AssetFilter(_search, stage, _categoryId, _locationId, _showInactive, page, pageSize, _statusId);
    }

    /// <summary>A changed filter always starts again at page 1.</summary>
    private async Task SearchAsync()
    {
        _page = 1;
        _csv = null;
        await LoadAsync();
    }

    private async Task PreviousAsync()
    {
        if (_page <= 1) return;
        _page--;
        await LoadAsync();
    }

    private async Task NextAsync()
    {
        if (_page >= Pages) return;
        _page++;
        await LoadAsync();
    }

    /// <summary>
    /// Builds the CSV for every asset that matches the filter (not just this page) and
    /// turns it into a data: link, so no JavaScript and no download endpoint are needed.
    /// </summary>
    private async Task PrepareCsvAsync()
    {
        _building = true;
        _error = null;

        try
        {
            var all = await Assets.SearchAsync(Filter(1, 0));

            var text = new System.Text.StringBuilder();
            text.AppendLine("Asset No.,Name,Category,Stage,Status,Location,Department,Custodian,Serial,Purchase date,Purchase cost,Active");

            foreach (var a in all.Items)
            {
                text.AppendLine(string.Join(',', new[]
                {
                    Csv.Cell(a.AssetNumber), Csv.Cell(a.Name), Csv.Cell(a.Category?.Name), Csv.Cell(a.LifecycleStage.Label()),
                    Csv.Cell(a.Status?.Name), Csv.Cell(a.Location?.Name), Csv.Cell(a.Department?.Name), Csv.Cell(a.Custodian?.Name),
                    Csv.Cell(a.SerialNumber), Csv.Cell(a.PurchaseDate?.ToString("yyyy-MM-dd")),
                    Csv.Cell(a.PurchaseCost?.ToString("0.00")), Csv.Cell(a.IsActive ? "Yes" : "No")
                }));
            }

            // The BOM makes Excel open the file as UTF-8.
            _csv = Csv.Link(text.ToString());
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _building = false; }
    }

    private void OpenAssign(Asset r)
    {
        _assign = r;
        _notice = null;
    }

    private async Task OnAssignedAsync(bool moved)
    {
        _assign = null;
        _notice = moved
            ? "The asset was assigned and is now in the employee's custody."
            : "The request was raised and is waiting for the administrator's approval.";

        await LoadAsync();
    }

    private async Task ToggleAsync(Asset r)
    {
        if (r.IsActive) await AssetCrud.DeactivateAsync(r.Id);
        else await AssetCrud.ActivateAsync(r.Id);
        await LoadAsync();
    }
}
