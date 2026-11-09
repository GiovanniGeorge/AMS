namespace AMS.Web.Components.Pages.Transactions;

public partial class IssuanceList
{
    [Inject] private AssetIssuanceService Issuance { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private List<AssetAssignment>? _rows;
    private Dictionary<int, string> _userNames = new();
    private string _status = string.Empty;
    private bool _isApprover;
    private int _total;
    private int _page = 1;


    private int? _rejectId;
    private string _reason = string.Empty;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
            _isApprover = await CurrentUser.IsInRoleAsync(AppRoles.Admin);
        }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Issuance.GetAllAsync(string.IsNullOrEmpty(_status) ? null : _status, _page);
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

    private async Task ApproveAsync(int id)
    {
        _error = null;
        try
        {
            await Issuance.ApproveAsync(id);
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private void OpenReject(int id)
    {
        _rejectId = id;
        _reason = string.Empty;
        _error = null;
    }

    private void CloseReject() => _rejectId = null;

    private async Task RejectAsync()
    {
        if (_rejectId is null) return;
        if (string.IsNullOrWhiteSpace(_reason)) { _error = "A rejection reason is required."; return; }

        try
        {
            await Issuance.RejectAsync(_rejectId.Value, _reason);
            _rejectId = null;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }
}
