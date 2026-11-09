namespace AMS.Web.Components.Shared;

public partial class QuickAddCategory
{
    [Parameter] public EventCallback<Category> OnAdded { get; set; }

    private string? _prefix;
    private decimal? _usefulLife;
    private decimal? _salvageValue;

    private string? Check()
    {
        if (_usefulLife is <= 0)
        {
            return "The useful life must be greater than zero, or left empty.";
        }

        if (_salvageValue is < 0)
        {
            return "The salvage value cannot be negative.";
        }

        return null;
    }

    private void Fill(Category category)
    {
        // All three are optional: a category with no prefix simply means the asset
        // number is typed by hand, and with no useful life it is not depreciated.
        category.NumberingPrefix = string.IsNullOrWhiteSpace(_prefix) ? null : _prefix.Trim();
        category.UsefulLife = _usefulLife;
        category.SalvageValue = _salvageValue;
    }

    private async Task AddedAsync(Category category)
    {
        _prefix = null;
        _usefulLife = null;
        _salvageValue = null;
        await OnAdded.InvokeAsync(category);
    }
}
