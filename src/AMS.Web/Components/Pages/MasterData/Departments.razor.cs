namespace AMS.Web.Components.Pages.MasterData;

public partial class Departments
{
    [Inject] private CrudService<Department> Service { get; set; } = default!;
    [Inject] private CrudService<User> UserService { get; set; } = default!;

    private List<Department>? _rows;
    private List<Department> _lookup = new();
    private Department? _editing;
    private string _title = string.Empty;
    private bool _showInactive;
    private string? _error;
    private Dictionary<int, string> _userNames = new();

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            _rows = await Service.GetAllAsync(_showInactive, d => d.ParentDepartment!); _userNames = (await UserService.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
            _lookup = await Service.GetAllAsync();
            _error = null;
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    private void OpenNew() { _editing = new Department(); _title = "New Department"; _error = null; }

    private async Task OpenEditAsync(int id)
    {
        _editing = await Service.GetByIdAsync(id);
        _title = "Edit Department";
        _error = null;
    }

    private void Cancel() { _editing = null; _error = null; }

    private async Task SaveAsync()
    {
        if (_editing is null) return;
        if (string.IsNullOrWhiteSpace(_editing.Name)) { _error = "Name is required."; return; }

        try
        {
            _editing.ParentDepartment = null;
            if (_editing.Id == 0) await Service.AddAsync(_editing);
            else await Service.UpdateAsync(_editing);

            _editing = null;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task ToggleAsync(Department r)
    {
        if (r.IsActive) await Service.DeactivateAsync(r.Id);
        else await Service.ActivateAsync(r.Id);
        await LoadAsync();
    }
}
