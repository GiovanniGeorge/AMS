namespace AMS.Web.Components.Pages;

public partial class Home
{
    [Inject] private CrudService<Asset> Assets { get; set; } = default!;
    [Inject] private AssetService AssetSearch { get; set; } = default!;
    [Inject] private CrudService<Category> Categories { get; set; } = default!;
    [Inject] private CrudService<Employee> Employees { get; set; } = default!;
    [Inject] private CrudService<Item> Items { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;
    [Inject] private AssetIssuanceService Issuance { get; set; } = default!;
    [Inject] private TransferService Transfers { get; set; } = default!;
    [Inject] private IncidentService Incidents { get; set; } = default!;
    [Inject] private DisposalService Disposals { get; set; } = default!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = default!;

    private int _assets, _categories, _employees, _items;
    private int _inStore, _assigned, _underMaintenance, _disposed;
    private bool _isApprover;

    private int _pendingIssuance, _pendingTransfers, _pendingIncidents, _pendingDisposals;
    private int _rejectedIssuance, _rejectedTransfers, _rejectedIncidents, _rejectedDisposals;
    private List<Incident> _losses = new();
    private Dictionary<int, string> _userNames = new();
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _assets = (await Assets.GetAllAsync()).Count;
            _categories = (await Categories.GetAllAsync()).Count;
            _employees = (await Employees.GetAllAsync()).Count;
            _items = (await Items.GetAllAsync()).Count;

            // PageSize 1 returns the count without dragging the rows back.
            _inStore = await CountAsync(LifecycleStage.InStore);
            _assigned = await CountAsync(LifecycleStage.Assigned);
            _underMaintenance = await CountAsync(LifecycleStage.UnderMaintenance);
            _disposed = await CountAsync(LifecycleStage.Disposed);
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
            _isApprover = await CurrentUser.IsInRoleAsync(AppRoles.Admin);
        }
        catch (Exception ex) { _error = ex.Message; }

        try
        {
            if (_isApprover)
            {
                _pendingIssuance = (await Issuance.GetPendingAsync()).Count;
                _pendingTransfers = (await Transfers.GetPendingAsync()).Count;
                _pendingIncidents = (await Incidents.GetPendingAsync()).Count;
                _pendingDisposals = (await Disposals.GetPendingAsync()).Count;
                _losses = await Incidents.GetLossNoticesAsync();
            }

            _rejectedIssuance = (await Issuance.GetMyRejectedAsync()).Count;
            _rejectedTransfers = (await Transfers.GetMyRejectedAsync()).Count;
            _rejectedIncidents = (await Incidents.GetMyRejectedAsync()).Count;
            _rejectedDisposals = (await Disposals.GetMyRejectedAsync()).Count;
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task<int> CountAsync(LifecycleStage stage) =>
        (await AssetSearch.SearchAsync(new AssetFilter(Stage: stage, PageSize: 1))).Total;

}
