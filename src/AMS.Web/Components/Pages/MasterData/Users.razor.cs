namespace AMS.Web.Components.Pages.MasterData;

public partial class Users
{
    [Inject] private DirectoryService Directory { get; set; } = default!;
    [Inject] private CrudService<User> Service { get; set; } = default!;
    [Inject] private CrudService<Role> RoleService { get; set; } = default!;
    [Inject] private CrudService<UserRole> UserRoleService { get; set; } = default!;
    [Inject] private CrudService<Employee> Employees { get; set; } = default!;
    [Inject] private Pbkdf2PasswordHasher Hasher { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private List<User>? _rows;
    private List<Role> _roles = new();
    private List<UserRole> _userRoles = new();
    private List<Employee> _employees = new();

    private User? _editing;
    private HashSet<int> _selectedRoleIds = new();
    private string _password = string.Empty;
    private string _title = string.Empty;

    private string? _search;
    private bool _showInactive;
    private string? _error;
    private Dictionary<int, string> _userNames = new();

    private int _page = 1;
    private int _total;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _userNames = (await Service.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
            _employees = await Employees.GetAllAsync();
        }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Directory.SearchUsersAsync(_search, _showInactive, _page);
            _rows = result.Items;
            _total = result.Total;

            _roles = await RoleService.GetAllAsync();
            _userRoles = await UserRoleService.GetAllAsync(false, ur => ur.Role!);
            _error = null;
        }
        catch (Exception ex) { _error = ex.Message; }
    }

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

    private string RoleNames(int userId)
    {
        var names = _userRoles
            .Where(ur => ur.UserId == userId && ur.Role is not null)
            .Select(ur => ur.Role!.Name);

        var joined = string.Join(", ", names);
        return joined.Length == 0 ? "—" : joined;
    }

    private void ToggleRole(int roleId, bool selected)
    {
        if (selected) _selectedRoleIds.Add(roleId);
        else _selectedRoleIds.Remove(roleId);
    }

    private void OpenNew()
    {
        _editing = new User();
        _selectedRoleIds = new HashSet<int>();
        _password = string.Empty;
        _title = "New User";
        _error = null;
    }

    private async Task OpenEditAsync(int id)
    {
        _editing = await Service.GetByIdAsync(id);
        _selectedRoleIds = _userRoles.Where(ur => ur.UserId == id).Select(ur => ur.RoleId).ToHashSet();
        _password = string.Empty;
        _title = "Edit User";
        _error = null;
    }

    private void Cancel()
    {
        _editing = null;
        _selectedRoleIds = new HashSet<int>();
        _password = string.Empty;
        _error = null;
    }

    private async Task SaveAsync()
    {
        if (_editing is null) return;
        if (string.IsNullOrWhiteSpace(_editing.Username)) { _error = "Username is required."; return; }
        if (_editing.Id == 0 && string.IsNullOrWhiteSpace(_password)) { _error = "Password is required for a new user."; return; }
        if (_editing.EmployeeId is null or 0) { _error = "Choose the employee this login belongs to."; return; }

        // Checked here so the clash is explained in words. The unique index in the database
        // is still what guarantees it, including against two people saving at the same moment.
        var taken = await Directory.LoginForEmployeeAsync(_editing.EmployeeId.Value, _editing.Id);
        if (taken is not null)
        {
            _error = $"This employee already has a login ({taken}). One person has one login - "
                     + "deactivate that account first, or pick another employee.";
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(_password))
            {
                _editing.PasswordHash = Hasher.Hash(_password);
            }

            // The navigation property is not saved - only the foreign key.
            _editing.Employee = null;

            if (_editing.Id == 0) await Service.AddAsync(_editing);
            else await Service.UpdateAsync(_editing);

            await SaveRolesAsync(_editing.Id);

            _editing = null;
            _selectedRoleIds = new HashSet<int>();
            _password = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    // Adds the newly ticked roles, re-activates previously removed ones,
    // and deactivates the ones that were unticked. Nothing is hard-deleted.
    private async Task SaveRolesAsync(int userId)
    {
        var existing = (await UserRoleService.GetAllAsync(true))
            .Where(ur => ur.UserId == userId)
            .ToList();

        foreach (var roleId in _selectedRoleIds)
        {
            var link = existing.FirstOrDefault(ur => ur.RoleId == roleId);

            if (link is null)
                await UserRoleService.AddAsync(new UserRole { UserId = userId, RoleId = roleId });
            else if (!link.IsActive)
                await UserRoleService.ActivateAsync(link.Id);
        }

        foreach (var link in existing.Where(ur => ur.IsActive && !_selectedRoleIds.Contains(ur.RoleId)))
        {
            await UserRoleService.DeactivateAsync(link.Id);
        }
    }

    private async Task ToggleAsync(User r)
    {
        if (r.IsActive) await Service.DeactivateAsync(r.Id);
        else await Service.ActivateAsync(r.Id);
        await LoadAsync();
    }

    private async Task QuickEmployeeAddedAsync(Employee created)
    {
        _employees = await Employees.GetAllAsync();
        _editing.EmployeeId = created.Id;
    }
}
