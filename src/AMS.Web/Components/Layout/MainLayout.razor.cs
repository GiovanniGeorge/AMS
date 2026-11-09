namespace AMS.Web.Components.Layout;

public partial class MainLayout
{
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool _navOpen;

    // forceLoad: the logout page must run as a plain HTTP request to remove the cookie.
    private void Logout() => Nav.NavigateTo("account/logout", forceLoad: true);

    private void ToggleNav() => _navOpen = !_navOpen;

    private void CloseNav() => _navOpen = false;
}
