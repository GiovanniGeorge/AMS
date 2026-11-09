namespace AMS.Web.Components.Pages.MasterData;

public partial class EmployeeDetails
{
    [Inject] private DirectoryService Directory { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    private Employee? _employee;
    private List<Asset> _assets = new();
    private List<CustodyLine> _history = new();

    private string CsvName => "custody-" + (_employee?.EmployeeNumber ?? "employee") + ".csv";

    private string EmployeeLabel => _employee is null
        ? string.Empty
        : _employee.EmployeeNumber + " — " + _employee.Name;

    private bool _loading = true;
    private bool _assigning;
    private string? _csv;
    private string? _error;
    private string? _notice;

    // Runs again when the route id changes, so one page serves every employee.
    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _csv = null;

        try
        {
            _employee = await Directory.GetEmployeeAsync(Id);

            if (_employee is not null)
            {
                _assets = await Directory.GetEmployeeAssetsAsync(Id);
                _history = await Directory.GetCustodyHistoryAsync(Id);
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
            ? "The asset was assigned and is now in this employee's custody."
            : "The request was raised and is waiting for the administrator's approval.";

        await LoadAsync();
    }

    /// <summary>The custody list as a data: link, same approach as the register - no endpoint, no JavaScript.</summary>
    private void PrepareCsv()
    {
        var text = new System.Text.StringBuilder();
        text.AppendLine("Asset No.,Name,Category,Stage,Condition,Location,Serial,Purchase date,Purchase cost");

        foreach (var a in _assets)
        {
            text.AppendLine(string.Join(',', new[]
            {
                Csv.Cell(a.AssetNumber), Csv.Cell(a.Name), Csv.Cell(a.Category?.Name), Csv.Cell(a.LifecycleStage.Label()),
                Csv.Cell(a.Status?.Name), Csv.Cell(a.Location?.Name), Csv.Cell(a.SerialNumber),
                Csv.Cell(a.PurchaseDate?.ToString("yyyy-MM-dd")), Csv.Cell(a.PurchaseCost?.ToString("0.00"))
            }));
        }

        _csv = Csv.Link(text.ToString());
    }

}
