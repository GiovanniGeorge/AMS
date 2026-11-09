namespace AMS.Web.Components.Pages.Maintenance;

public partial class MaintenancePlanList
{
    [Inject] private MaintenancePlanService Plans { get; set; } = default!;
    [Inject] private AssetService Assets { get; set; } = default!;
    [Inject] private CrudService<Category> Categories { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private List<PreventiveMaintenancePlan>? _rows;
    private List<DuePlanItem> _due = new();
    private List<Asset> _assets = new();
    private List<Category> _categories = new();
    private Dictionary<int, string> _userNames = new();

    private bool _open;
    private int? _editId;
    private string? _name;
    private int? _assetId;
    private int? _categoryId;
    private string _frequency = PlanFrequency.Monthly;
    private string? _scheduleNotes;
    private decimal? _usageThreshold;
    private DateTime? _startDate = DateTime.Today;
    private DateTime? _endDate;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _assets = (await Assets.SearchAsync(new AssetFilter(PageSize: 0))).Items;
            _categories = await Categories.GetAllAsync();
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
        }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _rows = await Plans.GetAllAsync();
            _due = await Plans.GetDueAsync();
            _error = null;
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    private void OpenNew()
    {
        _editId = null;
        _name = null;
        _assetId = null;
        _categoryId = null;
        _frequency = PlanFrequency.Monthly;
        _scheduleNotes = null;
        _usageThreshold = null;
        _startDate = DateTime.Today;
        _endDate = null;
        _error = null;
        _open = true;
    }

    private void OpenEdit(PreventiveMaintenancePlan plan)
    {
        _editId = plan.Id;
        _name = plan.Name;
        _assetId = plan.AssetId;
        _categoryId = plan.CategoryId;
        _frequency = plan.Frequency ?? PlanFrequency.Monthly;
        _scheduleNotes = plan.RepeatSchedule;
        _usageThreshold = plan.UsageThreshold;
        _startDate = plan.StartDate;
        _endDate = plan.EndDate;
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    // An asset and a category together would be two different scopes in one plan.
    private void OnAssetChosen() { if (_assetId is not null) _categoryId = null; }
    private void OnCategoryChosen() { if (_categoryId is not null) _assetId = null; }

    private async Task SaveAsync()
    {
        _error = null;
        if (string.IsNullOrWhiteSpace(_name)) { _error = "The name is required."; return; }
        if (_assetId is null && _categoryId is null) { _error = "Choose an asset or a category."; return; }

        try
        {
            var input = new MaintenancePlanInput(
                _name!, _assetId, _categoryId, _frequency, _scheduleNotes, _usageThreshold, _startDate, _endDate);

            if (_editId is int id) await Plans.UpdateAsync(id, input);
            else await Plans.CreateAsync(input);

            _open = false;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task OpenWorkOrderAsync(DuePlanItem item)
    {
        _error = null;
        try
        {
            await Plans.OpenWorkOrderAsync(item.PlanId, item.AssetId);
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task QuickCategoryAddedAsync(Category created)
    {
        _categories = await Categories.GetAllAsync();
        _categoryId = created.Id;
    }
}
