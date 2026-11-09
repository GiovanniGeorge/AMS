namespace AMS.Web.Components.Pages.Inventory;

public partial class StockBalanceList
{
    [Inject] private StockService Stock { get; set; } = default!;

    private List<StockBalance>? _rows;
    private bool _lowOnly;
    private string? _error;

    private List<StockBalance> Visible =>
        _rows is null ? new() : _lowOnly ? _rows.Where(IsBelowMinimum).ToList() : _rows;

    protected override async Task OnInitializedAsync()
    {
        try { _rows = await Stock.GetBalancesAsync(); }
        catch (Exception ex) { _error = ex.Message; }
    }

    private static bool IsBelowMinimum(StockBalance b) =>
        b.Item?.MinimumLevel is decimal min && b.Quantity < min;

    private static bool IsAtReorder(StockBalance b) =>
        b.Item?.ReorderPoint is decimal point && b.Quantity <= point;
}
