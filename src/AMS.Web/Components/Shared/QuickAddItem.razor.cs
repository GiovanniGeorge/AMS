namespace AMS.Web.Components.Shared;

public partial class QuickAddItem
{
    [Inject] private CrudService<Unit> Units { get; set; } = default!;

    [Parameter] public EventCallback<Item> OnAdded { get; set; }

    private List<Unit> _units = new();
    private int? _unitId;

    protected override async Task OnInitializedAsync()
    {
        try { _units = await Units.GetAllAsync(); }
        catch { /* handled by the validation below: without a unit nothing is saved */ }
    }

    private string? Check() =>
        _unitId is null ? "Choose the unit this item is counted in." : null;

    private void Fill(Item item) => item.UnitOfMeasureId = _unitId ?? 0;

    private async Task AddedAsync(Item item)
    {
        _unitId = null;
        await OnAdded.InvokeAsync(item);
    }
}
