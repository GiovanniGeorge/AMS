namespace AMS.Web.Components.Pages.Assets;

public partial class AssetImport
{
    [Inject] private AssetImportService Import { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>5 MB, the same ceiling as an attachment.</summary>
    private const long MaxBytes = 5 * 1024 * 1024;

    private ImportPreview? _preview;
    private ImportResult? _result;
    private string? _rejectedLink;
    private string? _error;
    private bool _busy;

    private string _templateLink = string.Empty;

    protected override void OnInitialized() =>
        _templateLink = Link(Import.BuildTemplate());

    private async Task OnFileAsync(InputFileChangeEventArgs e)
    {
        _error = null;
        _preview = null;
        _result = null;
        _rejectedLink = null;
        _busy = true;

        try
        {
            var file = e.File;

            if (file.Size > MaxBytes)
            {
                _error = "The file is larger than 5 MB.";
                return;
            }

            // Copied into memory first: ClosedXML needs a stream it can seek.
            using var memory = new MemoryStream();
            await file.OpenReadStream(MaxBytes).CopyToAsync(memory);
            memory.Position = 0;

            _preview = await Import.ReadAsync(memory, file.Name);

            if (_preview.FileProblem is not null)
            {
                _error = _preview.FileProblem;
                _preview = null;
            }
        }
        catch (Exception ex)
        {
            _error = ex.InnerException?.Message ?? ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RunAsync()
    {
        if (_preview is null) return;

        _error = null;
        _busy = true;

        try
        {
            _result = await Import.ImportAsync(_preview.Rows);
            _preview = null;

            _rejectedLink = _result.Rejected.Count == 0
                ? null
                : Link(Import.BuildRejectedWorkbook(_result.Rejected));
        }
        catch (Exception ex)
        {
            _error = ex.InnerException?.Message ?? ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private void Reset()
    {
        _preview = null;
        _result = null;
        _rejectedLink = null;
        _error = null;
    }

    /// <summary>A workbook as a data: link - the same no-endpoint approach the CSV exports use.</summary>
    private static string Link(byte[] workbook) =>
        "data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,"
        + Convert.ToBase64String(workbook);
}
