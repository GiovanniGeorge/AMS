using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using AMS.Application.Common;

namespace AMS.Web.Services;

/// <summary>
/// Reads the logged-in user's Id from the login cookie's claims.
/// Uses AuthenticationStateProvider, not HttpContext: in Blazor Server, HttpContext only
/// exists for the first HTTP request, not for the live connection that follows.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider _authenticationState;

    public CurrentUserService(AuthenticationStateProvider authenticationState)
    {
        _authenticationState = authenticationState;
    }

    public async Task<int?> GetUserIdAsync()
    {
        try
        {
            var state = await _authenticationState.GetAuthenticationStateAsync();
            var idText = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idText, out var userId) ? userId : null;
        }
        catch (InvalidOperationException)
        {
            // No authentication state is available (e.g. EF design-time tools).
            return null;
        }
    }

    public async Task<bool> IsInRoleAsync(string role)
    {
        try
        {
            var state = await _authenticationState.GetAuthenticationStateAsync();
            return state.User.FindAll(ClaimTypes.Role)
                .Any(c => string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase));
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
