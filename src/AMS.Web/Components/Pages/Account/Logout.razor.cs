using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AMS.Web.Components.Pages.Account;

public partial class Logout
{
    [Inject] private AccountService Account { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Reached without HTTP (in-app navigation): AuthLayout reloads the page.
        if (HttpContext is null) return;

        var idText = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idText, out var userId))
        {
            await Account.LogLogoutAsync(userId);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        Nav.NavigateTo("account/login");
    }
}
