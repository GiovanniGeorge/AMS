namespace AMS.Web.Components.Shared;

public partial class RedirectToLogin
{
    [Inject] private NavigationManager Nav { get; set; } = default!;

    protected override void OnInitialized()
    {
        var returnUrl = "/" + Nav.ToBaseRelativePath(Nav.Uri);
        Nav.NavigateTo($"account/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
    }
}
