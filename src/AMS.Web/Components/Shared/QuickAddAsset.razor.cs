namespace AMS.Web.Components.Shared;

public partial class QuickAddAsset
{
    [Inject] private AssetService Assets { get; set; } = default!;
    [Inject] private CrudService<Category> Categories { get; set; } = default!;
    [Inject] private CrudService<AssetStatus> Statuses { get; set; } = default!;

    /// <summary>The registered asset, so the caller can refresh its list and select it.</summary>
    [Parameter] public EventCallback<Asset> OnAdded { get; set; }

    private List<Category> _categories = new();
    private List<AssetStatus> _statuses = new();

    private string? _name;
    private int? _categoryId;
    private int? _statusId;
    private string? _preview;

    private bool _open;
    private bool _saving;
    private string? _error;

    private async Task Open()
    {
        _open = true;
        _name = null;
        _categoryId = null;
        _statusId = null;
        _preview = null;
        _error = null;

        try
        {
            _categories = await Categories.GetAllAsync();
            _statuses = await Statuses.GetAllAsync();
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    private void Cancel()
    {
        _open = false;
        _error = null;
    }

    private async Task PreviewAsync()
    {
        _preview = _categoryId is int id ? await Assets.PreviewNextNumberAsync(id) : null;
    }

    private async Task SaveAsync()
    {
        _error = null;

        if (string.IsNullOrWhiteSpace(_name)) { _error = "Type the asset name."; return; }
        if (_categoryId is null) { _error = "Choose a category."; return; }
        if (_statusId is null) { _error = "Choose the condition."; return; }

        _saving = true;

        try
        {
            var asset = new Asset
            {
                Name = _name!.Trim(),
                CategoryId = _categoryId.Value,
                StatusId = _statusId.Value
            };

            var id = await Assets.RegisterAsync(asset);
            asset.Id = id;

            _open = false;
            await OnAdded.InvokeAsync(asset);
        }
        catch (Exception ex)
        {
            _error = ex.InnerException?.Message ?? ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }
}
