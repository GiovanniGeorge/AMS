namespace AMS.Web.Components.Pages.Assets;

public partial class VerificationList
{
    [Inject] private VerificationService Campaigns { get; set; } = default!;
    [Inject] private CrudService<Location> Locations { get; set; } = default!;
    [Inject] private CrudService<Department> Departments { get; set; } = default!;
    [Inject] private CrudService<Category> Categories { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;

    private List<VerificationCampaign>? _rows;
    private List<Location> _locations = new();
    private List<Department> _departments = new();
    private List<Category> _categories = new();
    private Dictionary<int, string> _userNames = new();

    private bool _open;
    private int? _editId;
    private string? _name;
    private int? _locationId;
    private int? _departmentId;
    private int? _categoryId;
    private DateTime? _startDate = DateTime.Today;
    private DateTime? _endDate;
    private int _scopeCount;

    private int? _resultsId;
    private decimal? _progress;
    private string? _results;
    private bool _closeCampaign;

    private VerificationCampaign? _snapshot;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _locations = await Locations.GetAllAsync();
            _departments = await Departments.GetAllAsync();
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
            _rows = await Campaigns.GetAllAsync();
            _error = null;
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    private void OpenNew()
    {
        _editId = null;
        _name = null;
        _locationId = null;
        _departmentId = null;
        _categoryId = null;
        _startDate = DateTime.Today;
        _endDate = null;
        _scopeCount = 0;
        _error = null;
        _open = true;
    }

    private async Task OpenEdit(VerificationCampaign campaign)
    {
        _editId = campaign.Id;
        _name = campaign.Name;
        _locationId = campaign.LocationId;
        _departmentId = campaign.DepartmentId;
        _categoryId = campaign.CategoryId;
        _startDate = campaign.StartDate;
        _endDate = campaign.EndDate;
        _error = null;
        _open = true;
        await CountAsync();
    }

    private void Close() => _open = false;

    private async Task CountAsync()
    {
        try { _scopeCount = await Campaigns.CountScopeAsync(_locationId, _departmentId, _categoryId); }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task SaveAsync()
    {
        _error = null;
        if (string.IsNullOrWhiteSpace(_name)) { _error = "The name is required."; return; }

        try
        {
            var input = new VerificationInput(_name!, _locationId, _departmentId, _categoryId, _startDate, _endDate);

            if (_editId is int id) await Campaigns.UpdateAsync(id, input);
            else await Campaigns.CreateAsync(input);

            _open = false;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task SnapshotAsync(int id)
    {
        _error = null;
        try
        {
            await Campaigns.TakeSnapshotAsync(id);
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private void OpenResults(VerificationCampaign campaign)
    {
        _resultsId = campaign.Id;
        _progress = campaign.Progress;
        _results = campaign.Results;
        _closeCampaign = false;
        _error = null;
    }

    private async Task SaveResultsAsync()
    {
        if (_resultsId is null) return;

        try
        {
            await Campaigns.SaveResultsAsync(_resultsId.Value, _progress, _results, _closeCampaign);
            _resultsId = null;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private string Scope(VerificationCampaign c)
    {
        var parts = new[] { c.Location?.Name, c.Department?.Name, c.Category?.Name }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var text = string.Join(" · ", parts);
        return text.Length == 0 ? "All assets" : text;
    }

    private static int Count(string snapshot) =>
        snapshot.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;

    private async Task QuickLocationAddedAsync(Location created)
    {
        _locations = await Locations.GetAllAsync();
        _locationId = created.Id;
    }

    private async Task QuickDepartmentAddedAsync(Department created)
    {
        _departments = await Departments.GetAllAsync();
        _departmentId = created.Id;
    }

    private async Task QuickCategoryAddedAsync(Category created)
    {
        _categories = await Categories.GetAllAsync();
        _categoryId = created.Id;
    }
}
