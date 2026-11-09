namespace AMS.Web.Components.Pages.Reports;

public partial class Purchases
{
    [Inject] private ReportService ReportService { get; set; } = default!;

    private DateTime _from = DateTime.Today.AddDays(-30);
    private DateTime _to = DateTime.Today;

    private PurchaseReport? _report;
    private bool _running;
    private string? _csv;
    private string? _error;

    private async Task RunAsync()
    {
        _error = null;
        _csv = null;

        if (_from == default || _to == default) { _error = "Choose both dates."; return; }
        if (_from > _to) { _error = "The start date must come before the end date."; return; }

        _running = true;

        try
        {
            _report = await ReportService.GetPurchasesAsync(_from, _to);
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _running = false; }
    }

    private async Task SetRangeAsync(int days)
    {
        _to = DateTime.Today;
        _from = DateTime.Today.AddDays(-days);
        await RunAsync();
    }

    private async Task SetThisYearAsync()
    {
        _to = DateTime.Today;
        _from = new DateTime(DateTime.Today.Year, 1, 1);
        await RunAsync();
    }

    /// <summary>Both tables in one file, same data: link approach as the rest of AMS.</summary>
    private void PrepareCsv()
    {
        if (_report is null) return;

        var text = new System.Text.StringBuilder();

        text.AppendLine($"Purchases {_report.From:yyyy-MM-dd} to {_report.To:yyyy-MM-dd}");
        text.AppendLine();
        text.AppendLine("ASSETS");
        text.AppendLine("Asset No.,Name,Category,Supplier,Date,Basis,Cost,Used,First used,Days idle,Held by,Stage");

        foreach (var a in _report.Assets)
        {
            text.AppendLine(string.Join(',', new[]
            {
                Csv.Cell(a.AssetNumber), Csv.Cell(a.Name), Csv.Cell(a.Category), Csv.Cell(a.Supplier),
                Csv.Cell(a.Date.ToString("yyyy-MM-dd")), Csv.Cell(a.DateBasis), Csv.Cell(a.Cost?.ToString("0.00")),
                Csv.Cell(a.Used ? "Used" : "Never used"), Csv.Cell(a.FirstUsedOn?.ToString("yyyy-MM-dd")),
                Csv.Cell(a.DaysToFirstUse?.ToString()), Csv.Cell(a.CurrentCustodian), Csv.Cell(a.Stage.Label())
            }));
        }

        text.AppendLine();
        text.AppendLine("STORE ITEMS");
        text.AppendLine("Item,Unit,Received,Consumed in the period,Difference");

        foreach (var i in _report.Items)
        {
            text.AppendLine(string.Join(',', new[]
            {
                Csv.Cell(i.Name), Csv.Cell(i.Unit), Csv.Cell(i.Received.ToString("0.00")),
                Csv.Cell(i.Consumed.ToString("0.00")), Csv.Cell((i.Received - i.Consumed).ToString("0.00"))
            }));
        }

        _csv = Csv.Link(text.ToString());
    }

}
