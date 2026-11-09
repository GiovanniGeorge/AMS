namespace AMS.Web.Components.Pages.MasterData;

public partial class Employees
{
    [Inject] private DirectoryService Directory { get; set; } = default!;
    [Inject] private CrudService<Employee> Service { get; set; } = default!;
    [Inject] private CrudService<User> UserService { get; set; } = default!;
    [Inject] private CrudService<Department> Departments { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private List<Employee>? _rows;
    private List<Department> _departments = new();
    private Dictionary<int, int> _counts = new();
    private Dictionary<int, string> _userNames = new();

    private Employee? _editing;
    private string _title = string.Empty;

    private string? _search;
    private int? _departmentId;
    private bool _showInactive;
    private string? _error;

    private int _page = 1;
    private int _total;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _departments = await Departments.GetAllAsync();
            _userNames = (await UserService.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
        }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Directory.SearchEmployeesAsync(
                new EmployeeFilter(_search, _departmentId, _showInactive, _page));

            _rows = result.Items;
            _total = result.Total;

            // One grouped query for the whole page, not one per row.
            _counts = await Directory.CountAssetsAsync(_rows.Select(r => r.Id));
            _error = null;
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    /// <summary>A changed filter always starts again at page 1.</summary>
    private async Task SearchAsync()
    {
        _page = 1;
        await LoadAsync();
    }

    private async Task GoToPageAsync(int page)
    {
        _page = page;
        await LoadAsync();
    }

    private void OpenNew() { _editing = new Employee(); _title = "New Employee"; _error = null; }

    private async Task OpenEditAsync(int id)
    {
        _editing = await Service.GetByIdAsync(id);
        _title = "Edit Employee";
        _error = null;
    }

    private void Cancel() { _editing = null; _error = null; }

    private async Task SaveAsync()
    {
        if (_editing is null) return;
        if (string.IsNullOrWhiteSpace(_editing.EmployeeNumber)) { _error = "Employee number is required."; return; }
        if (string.IsNullOrWhiteSpace(_editing.Name)) { _error = "Name is required."; return; }

        try
        {
            _editing.Department = null;
            if (_editing.Id == 0) await Service.AddAsync(_editing);
            else await Service.UpdateAsync(_editing);

            _editing = null;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task ToggleAsync(Employee r)
    {
        if (r.IsActive) await Service.DeactivateAsync(r.Id);
        else await Service.ActivateAsync(r.Id);
        await LoadAsync();
    }

    private async Task QuickDepartmentAddedAsync(Department created)
    {
        _departments = await Departments.GetAllAsync();
        _departmentId = created.Id;
    }
}
