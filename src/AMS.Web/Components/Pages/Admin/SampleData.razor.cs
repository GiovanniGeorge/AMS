using AMS.Infrastructure.Services;
using Microsoft.AspNetCore.Components;

namespace AMS.Web.Components.Pages.Admin;

/// <summary>The code half of SampleData.razor. Both halves compile into one class.</summary>
public partial class SampleData
{
    [Inject] private SampleDataService Sample { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool? _canLoad;
    private string? _done;
    private string? _error;
    private bool _busy;

    protected override async Task OnInitializedAsync()
    {
        try { _canLoad = await Sample.CanLoadAsync(); }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task LoadAsync()
    {
        _error = null;
        _busy = true;

        try
        {
            _done = await Sample.LoadAsync();
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
}
