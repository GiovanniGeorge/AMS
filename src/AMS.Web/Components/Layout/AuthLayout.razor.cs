namespace AMS.Web.Components.Layout;

public partial class AuthLayout
{
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    protected override void OnParametersSet()
    {
        // Account pages must run as plain HTTP pages (they write the login cookie).
        // If one was reached through in-app navigation, e.g. the browser Back button,
        // there is no HttpContext - reload it as a normal HTTP page.
        if (HttpContext is null)
        {
            Nav.Refresh(forceReload: true);
        }
    }
}
