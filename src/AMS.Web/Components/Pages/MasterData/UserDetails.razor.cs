namespace AMS.Web.Components.Pages.MasterData;

public partial class UserDetails
{
    [Inject] private DirectoryService Directory { get; set; } = default!;
    [Inject] private CrudService<UserRole> UserRoleService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    private User? _user;
    private List<Asset> _assets = new();
    private string _roles = "—";

    private bool _loading = true;
    private bool _assigning;
    private string? _error;
    private string? _notice;

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;

        try
        {
            _user = await Directory.GetUserAsync(Id);

            if (_user is not null)
            {
                var links = await UserRoleService.GetAllAsync(false, ur => ur.Role!);
                var names = links.Where(ur => ur.UserId == Id && ur.Role is not null).Select(ur => ur.Role!.Name);
                _roles = string.Join(", ", names);
                if (_roles.Length == 0) _roles = "—";

                _assets = _user.EmployeeId is { } employeeId
                    ? await Directory.GetEmployeeAssetsAsync(employeeId)
                    : new List<Asset>();
            }

            _error = null;
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _loading = false; }
    }

    private async Task OnAssignedAsync(bool moved)
    {
        _assigning = false;
        _notice = moved
            ? "The asset was assigned and is now in this person's custody."
            : "The request was raised and is waiting for the administrator's approval.";

        await LoadAsync();
    }
}
