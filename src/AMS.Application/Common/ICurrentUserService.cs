namespace AMS.Application.Common;

/// <summary>
/// Tells the rest of the system who is logged in.
/// Lives in Application so Infrastructure (the DbContext) can fill CreatedById / ModifiedById
/// without depending on Blazor. The implementation is in AMS.Web.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The logged-in user's Id, or null when nobody is logged in.</summary>
    Task<int?> GetUserIdAsync();

    /// <summary>True when the logged-in user has the role (case-insensitive). Roles are read at login.</summary>
    Task<bool> IsInRoleAsync(string role);
}
