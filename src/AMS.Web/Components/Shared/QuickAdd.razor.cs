namespace AMS.Web.Components.Shared;

public partial class QuickAdd<TItem>
{
    [Inject] private CrudService<TItem> Service { get; set; } = default!;

    /// <summary>What is being added, in lower case: "supplier", "location", "item".</summary>
    [Parameter] public string What { get; set; } = "record";

    /// <summary>Extra inputs this lookup needs — an item's unit of measure, for example.</summary>
    [Parameter] public RenderFragment? Extra { get; set; }

    /// <summary>Fills whatever Extra collected onto the new row, just before it is saved.</summary>
    [Parameter] public Action<TItem>? Prepare { get; set; }

    /// <summary>Validates what Extra collected; return a message to stop the save.</summary>
    [Parameter] public Func<string?>? Validate { get; set; }

    /// <summary>The saved row, so the caller can refresh its list and select it.</summary>
    [Parameter] public EventCallback<TItem> OnAdded { get; set; }

    private bool _open;
    private bool _saving;
    private string? _name;
    private string? _error;

    private void Open()
    {
        _open = true;
        _name = null;
        _error = null;
    }

    private void Cancel()
    {
        _open = false;
        _error = null;
    }

    private async Task SaveAsync()
    {
        _error = null;

        if (string.IsNullOrWhiteSpace(_name))
        {
            _error = $"Type the {What} name.";
            return;
        }

        if (Validate?.Invoke() is { } problem)
        {
            _error = problem;
            return;
        }

        _saving = true;

        try
        {
            var created = new TItem { Name = _name!.Trim() };
            Prepare?.Invoke(created);

            await Service.AddAsync(created);

            _open = false;
            await OnAdded.InvokeAsync(created);
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
