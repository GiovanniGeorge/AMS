namespace AMS.Web.Components.Pages.Admin;

public partial class AuditLogList
{
    [Inject] private AuditLogService Audit { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;

    private const int PageSize = 50;

    private List<AuditLog>? _rows;
    private List<User> _users = new();
    private List<string> _entityNames = new();

    private string _action = string.Empty;
    private string _entityName = string.Empty;
    private int? _userId;
    private DateTime? _from;
    private DateTime? _to;

    private int _total;
    private int _page = 1;
    private string? _error;

    private int Pages => _total == 0 ? 1 : (_total + PageSize - 1) / PageSize;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _users = await Users.GetAllAsync(true);
            _entityNames = await Audit.GetEntityNamesAsync();
        }
        catch (Exception ex) { _error = ex.Message; }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            AuditAction? action = Enum.TryParse<AuditAction>(_action, out var a) ? a : null;

            var result = await Audit.SearchAsync(new AuditFilter(
                action,
                string.IsNullOrEmpty(_entityName) ? null : _entityName,
                _userId,
                _from,
                _to,
                _page,
                PageSize));

            _rows = result.Items;
            _total = result.Total;
            _error = null;
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task SearchAsync()
    {
        _page = 1;
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

    private static string Short(string? json)
    {
        if (string.IsNullOrEmpty(json)) return "—";
        return json.Length <= 160 ? json : json[..160] + "…";
    }
}
