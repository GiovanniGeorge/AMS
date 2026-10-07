namespace AMS.Application.Common;

/// <summary>The data put into the login cookie after a successful login.</summary>
public record LoginResult(int UserId, string Username, IReadOnlyList<string> Roles);

/// <summary>Login, logout and first-user setup.</summary>
public interface IAccountService
{
    /// <summary>
    /// Checks the username and password of an active user.
    /// Writes a Login or FailedLogin row to AuditLog. Returns null when the login fails.
    /// </summary>
    Task<LoginResult?> ValidateCredentialsAsync(string username, string password);

    /// <summary>Writes a Logout row to AuditLog.</summary>
    Task LogLogoutAsync(int userId);

    /// <summary>True as soon as at least one user exists (active or not).</summary>
    Task<bool> AnyUsersExistAsync();

    /// <summary>
    /// Creates the very first user. Returns false, and creates nothing,
    /// if any user already exists.
    /// </summary>
    Task<bool> CreateFirstUserAsync(string username, string password);
}
