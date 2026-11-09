namespace AMS.Web.Components.Pages.Assets;

public partial class AssetDetails
{
    [Inject] private AssetService Assets { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;
    [Inject] private AttachmentService Attachments { get; set; } = default!;
    [Inject] private InvoiceService Invoices { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    private Asset? _asset;
    private List<Invoice> _invoices = new();
    private List<Asset> _components = new();
    private List<AssetHistoryEntry> _history = new();
    private Dictionary<int, string> _userNames = new();
    private List<AttachmentInfo> _files = new();
    private readonly Dictionary<int, string> _ready = new();   // id -> data: link, built on request
    private string? _documentType;
    private string? _fileError;
    private bool _notFound;
    private string? _error;

    private const string EntityType = "Asset";

    // Id changes when moving from one asset to another (e.g. clicking a component).
    protected override async Task OnParametersSetAsync()
    {
        try
        {
            _asset = await Assets.GetDetailsAsync(Id);
            _invoices = await Invoices.GetForAssetAsync(Id);
            _notFound = _asset is null;
            if (_asset is null) return;

            _components = await Assets.GetComponentsAsync(Id);
            _history = await Assets.GetHistoryAsync(Id);
            _files = await Attachments.GetForAsync(EntityType, Id);
            _ready.Clear();
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
            _error = null;
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task UploadAsync(InputFileChangeEventArgs e)
    {
        _fileError = null;
        var file = e.File;

        if (file is null) return;

        if (file.Size > AttachmentService.MaxBytes)
        {
            _fileError = "The file is larger than 5 MB.";
            return;
        }

        try
        {
            using var stream = file.OpenReadStream(AttachmentService.MaxBytes);
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);

            await Attachments.AddAsync(new AttachmentInput(
                EntityType, Id, file.Name, file.ContentType, _documentType, memory.ToArray()));

            _documentType = null;
            _files = await Attachments.GetForAsync(EntityType, Id);
        }
        catch (Exception ex) { _fileError = ex.InnerException?.Message ?? ex.Message; }
    }

    /// <summary>
    /// Turns the stored bytes into a data: link. Done on request, not for the whole list,
    /// so opening an asset with ten photos does not pull ten files.
    /// </summary>
    private async Task PrepareAsync(int attachmentId)
    {
        _fileError = null;

        try
        {
            var file = await Attachments.GetWithContentAsync(attachmentId);
            if (file?.Content is null) { _fileError = "The file is no longer there."; return; }

            var type = string.IsNullOrWhiteSpace(file.FileType) ? "application/octet-stream" : file.FileType;
            _ready[attachmentId] = $"data:{type};base64,{Convert.ToBase64String(file.Content)}";
        }
        catch (Exception ex) { _fileError = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task RemoveFileAsync(int attachmentId)
    {
        _fileError = null;

        try
        {
            await Attachments.DeactivateAsync(attachmentId);
            _ready.Remove(attachmentId);
            _files = await Attachments.GetForAsync(EntityType, Id);
        }
        catch (Exception ex) { _fileError = ex.InnerException?.Message ?? ex.Message; }
    }

    private static string Size(long? bytes) => bytes switch
    {
        null => "—",
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024d:N0} KB",
        _ => $"{bytes / (1024d * 1024d):N1} MB"
    };

    private static string Show(params string?[] parts)
    {
        var text = string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return text.Length == 0 ? "—" : text;
    }

    private static string? Range(DateTime? from, DateTime? to) =>
        from is null && to is null ? null : $"{from:yyyy-MM-dd} → {to:yyyy-MM-dd}";
}
