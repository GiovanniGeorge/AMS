namespace AMS.Web.Components.Pages.MasterData;

public partial class Units
{
    [Inject] private CrudService<Unit> Service { get; set; } = default!;
    [Inject] private CrudService<User> UserService { get; set; } = default!;

    private List<Unit>? _rows;
    private Unit? _editing;
    private string _title = string.Empty;
    private bool _showInactive;
    private string? _error;
    private Dictionary<int, string> _userNames = new();

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        try { _rows = await Service.GetAllAsync(_showInactive); _userNames = (await UserService.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username); _error = null; }
        catch (Exception ex) { _error = ex.Message; }
    }

    private void OpenNew() { _editing = new Unit(); _title = "New Unit"; _error = null; }

    private async Task OpenEditAsync(int id)
    {
        _editing = await Service.GetByIdAsync(id);
        _title = "Edit Unit";
        _error = null;
    }

    private void Cancel() { _editing = null; _error = null; }

    private async Task SaveAsync()
    {
        if (_editing is null) return;
        if (string.IsNullOrWhiteSpace(_editing.Name)) { _error = "Name is required."; return; }

        try
        {
            if (_editing.Id == 0) await Service.AddAsync(_editing);
            else await Service.UpdateAsync(_editing);

            _editing = null;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task ToggleAsync(Unit r)
    {
        if (r.IsActive) await Service.DeactivateAsync(r.Id);
        else await Service.ActivateAsync(r.Id);
        await LoadAsync();
    }
}
