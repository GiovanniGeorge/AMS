using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AMS.Web.Components.Pages.Account;

public partial class Login
{
    [Inject] private AccountService Account { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [SupplyParameterFromForm]
    private LoginModel Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery]
    private string? Created { get; set; }

    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        // Reached without HTTP (in-app navigation): AuthLayout reloads the page.
        if (HttpContext is null) return;

        // No users yet: the first one is created on the setup page.
        if (!await Account.AnyUsersExistAsync())
        {
            Nav.NavigateTo("account/setup");
        }
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Username) || string.IsNullOrWhiteSpace(Input.Password))
        {
            _error = "Enter your username and password.";
            return;
        }

        var result = await Account.ValidateCredentialsAsync(Input.Username.Trim(), Input.Password);
        if (result is null)
        {
            _error = "Invalid username or password.";
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new(ClaimTypes.Name, result.Username)
        };
        claims.AddRange(result.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        // IsPersistent: the cookie survives closing the browser (default lifetime 14 days).
        await HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

        // Do NOT put this inside try/catch: on a plain HTTP page NavigateTo works by
        // throwing, and the framework turns that into a redirect.
        Nav.NavigateTo(GetSafeReturnUrl());
    }

    // Only redirect inside this site - never to another website ("open redirect").
    private string GetSafeReturnUrl()
    {
        if (!string.IsNullOrEmpty(ReturnUrl)
            && ReturnUrl.StartsWith('/')
            && !ReturnUrl.StartsWith("//")
            && !ReturnUrl.StartsWith("/\\"))
        {
            return ReturnUrl;
        }

        return "/";
    }

    private sealed class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
