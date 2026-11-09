using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

/// <summary>Login, logout and first-user setup.</summary>
public class AccountService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly Pbkdf2PasswordHasher _hasher;

    public AccountService(IDbContextFactory<ApplicationDbContext> dbFactory, Pbkdf2PasswordHasher hasher)
    {
        _dbFactory = dbFactory;
        _hasher = hasher;
    }

    /// <summary>
    /// Checks the username and password of an active user.
    /// Writes a Login or FailedLogin row to AuditLog. Returns null when the login fails.
    /// </summary>
    public async Task<LoginResult?> ValidateCredentialsAsync(string username, string password)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles.Where(ur => ur.IsActive))
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username);

        // Inactive users cannot log in.
        var isValid = user is not null
                      && user.IsActive
                      && _hasher.Verify(user.PasswordHash, password);

        db.AuditLogs.Add(new AuditLog
        {
            UserId = user?.Id,
            Action = isValid ? AuditAction.Login : AuditAction.FailedLogin,
            EntityName = nameof(User),
            RecordId = user?.Id.ToString(),
            DateTime = DateTime.UtcNow,
            LogLevel = isValid ? "Information" : "Warning"
        });
        await db.SaveChangesAsync();

        if (!isValid)
        {
            return null;
        }

        var roles = user!.UserRoles
            .Where(ur => ur.Role is { IsActive: true })
            .Select(ur => ur.Role!.Name)
            .ToList();

        return new LoginResult(user.Id, user.Username, roles);
    }

    /// <summary>Writes a Logout row to AuditLog.</summary>
    public async Task LogLogoutAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.Logout,
            EntityName = nameof(User),
            RecordId = userId.ToString(),
            DateTime = DateTime.UtcNow,
            LogLevel = "Information"
        });
        await db.SaveChangesAsync();
    }

    /// <summary>True as soon as at least one user exists (active or not).</summary>
    public async Task<bool> AnyUsersExistAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Users.AnyAsync();
    }

    /// <summary>
    /// Creates the very first user. Returns false, and creates nothing,
    /// if any user already exists.
    /// </summary>
    public async Task<bool> CreateFirstUserAsync(string username, string password)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Checked again here, not only on the page, so the setup can never run twice.
        if (await db.Users.AnyAsync())
        {
            return false;
        }

        var user = new User
        {
            Username = username,
            PasswordHash = _hasher.Hash(password),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // The Users and Roles screens are for the administrator only, so the very first
        // account has to be one - otherwise nobody could ever open them again.
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Admin);
        if (role is null)
        {
            role = new Role { Name = AppRoles.Admin, IsActive = true };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, IsActive = true });
        await db.SaveChangesAsync();
        return true;
    }
}
