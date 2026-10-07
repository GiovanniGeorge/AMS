using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;

    public AccountService(ApplicationDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<LoginResult?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles.Where(ur => ur.IsActive))
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username);

        // Inactive users cannot log in.
        var isValid = user is not null
                      && user.IsActive
                      && _hasher.Verify(user.PasswordHash, password);

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user?.Id,
            Action = isValid ? AuditAction.Login : AuditAction.FailedLogin,
            EntityName = nameof(User),
            RecordId = user?.Id.ToString(),
            DateTime = DateTime.UtcNow,
            LogLevel = isValid ? "Information" : "Warning"
        });
        await _db.SaveChangesAsync();

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

    public async Task LogLogoutAsync(int userId)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.Logout,
            EntityName = nameof(User),
            RecordId = userId.ToString(),
            DateTime = DateTime.UtcNow,
            LogLevel = "Information"
        });
        await _db.SaveChangesAsync();
    }

    public Task<bool> AnyUsersExistAsync()
    {
        return _db.Users.AnyAsync();
    }

    public async Task<bool> CreateFirstUserAsync(string username, string password)
    {
        // Checked again here, not only on the page, so the setup can never run twice.
        if (await _db.Users.AnyAsync())
        {
            return false;
        }

        var user = new User
        {
            Username = username,
            PasswordHash = _hasher.Hash(password),
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // The Users and Roles screens are for the administrator only, so the very first
        // account has to be one - otherwise nobody could ever open them again.
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Admin);
        if (role is null)
        {
            role = new Role { Name = AppRoles.Admin, IsActive = true };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
        }

        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, IsActive = true });
        await _db.SaveChangesAsync();
        return true;
    }
}
