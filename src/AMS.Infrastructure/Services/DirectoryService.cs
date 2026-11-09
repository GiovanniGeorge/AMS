using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Application.MasterData;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

/// <summary>
/// Searching the two people tables, plus the question the Employees screen exists
/// to answer: what is this person holding right now?
/// </summary>
public class DirectoryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public DirectoryService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ------------------------------------------------------------------
    // Employees
    // ------------------------------------------------------------------

    public async Task<Paged<Employee>> SearchEmployeesAsync(EmployeeFilter filter)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<Employee> query = db.Employees
            .AsNoTracking()
            .Include(e => e.Department);

        if (!filter.IncludeInactive)
        {
            query = query.Where(e => e.IsActive);
        }

        if (filter.DepartmentId is { } departmentId)
        {
            query = query.Where(e => e.DepartmentId == departmentId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var text = filter.Search.Trim();
            query = query.Where(e => e.Name.Contains(text) || e.EmployeeNumber.Contains(text));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(e => e.Name)
            .Skip((Math.Max(filter.Page, 1) - 1) * Paged<Employee>.Size)
            .Take(Paged<Employee>.Size)
            .ToListAsync();

        return new Paged<Employee>(items, total);
    }

    /// <summary>One employee with the department loaded.</summary>
    public async Task<Employee?> GetEmployeeAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// The assets the employee holds right now - read from Asset.CustodianId,
    /// which is the single source of truth for current custody.
    /// </summary>
    public async Task<List<Asset>> GetEmployeeAssetsAsync(int employeeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Assets
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Status)
            .Include(a => a.Location)
            .Include(a => a.Department)
            .Where(a => a.IsActive && a.CustodianId == employeeId)
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    /// <summary>Every approved custody this employee has ever had, newest first.</summary>
    public async Task<List<CustodyLine>> GetCustodyHistoryAsync(int employeeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // No Include here on purpose: the Select below decides what is read,
        // and EF would only warn that the Includes were ignored.
        return await db.AssetAssignments
            .AsNoTracking()
            .Where(a => a.IsActive
                        && a.CustodianId == employeeId
                        && a.Status == RequestStatus.Approved
                        && a.Asset != null)
            .OrderByDescending(a => a.Id)
            .Select(a => new CustodyLine(
                a.AssetId,
                a.Asset!.AssetNumber,
                a.Asset.Name,
                a.Asset.Category != null ? a.Asset.Category.Name : null,
                a.Asset.Status != null ? a.Asset.Status.Name : null,
                a.Asset.LifecycleStage,
                a.Location != null ? a.Location.Name : null,
                a.AssignmentDate,
                a.ReturnDate,
                a.ReturnDate == null && a.Asset.CustodianId == employeeId))
            .ToListAsync();
    }

    /// <summary>How many assets each of these employees holds right now, by employee id.</summary>
    public async Task<Dictionary<int, int>> CountAssetsAsync(IEnumerable<int> employeeIds)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ids = employeeIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        // Grouped in the database, one round trip for the whole page.
        var counts = await db.Assets
            .AsNoTracking()
            .Where(a => a.IsActive && a.CustodianId != null && ids.Contains(a.CustodianId.Value))
            .GroupBy(a => a.CustodianId!.Value)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(c => c.EmployeeId, c => c.Count);
    }

    // ------------------------------------------------------------------
    // Users
    // ------------------------------------------------------------------

    /// <summary>Login accounts, searched by username, with the linked employee loaded.</summary>
    public async Task<Paged<User>> SearchUsersAsync(string? search, bool includeInactive, int page)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<User> query = db.Users
            .AsNoTracking()
            .Include(u => u.Employee);

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(u => u.Username.Contains(text)
                                     || (u.Employee != null && u.Employee.Name.Contains(text)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.Username)
            .Skip((Math.Max(page, 1) - 1) * Paged<User>.Size)
            .Take(Paged<User>.Size)
            .ToListAsync();

        return new Paged<User>(items, total);
    }

    /// <summary>
    /// The username of the active login already linked to this employee, or null when the
    /// person has none. <paramref name="exceptUserId"/> is the account being edited, so it
    /// is not reported as a clash with itself.
    /// </summary>
    public async Task<string?> LoginForEmployeeAsync(int employeeId, int exceptUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.EmployeeId == employeeId && u.Id != exceptUserId)
            .Select(u => u.Username)
            .FirstOrDefaultAsync();
    }

    /// <summary>One login account with its linked employee.</summary>
    public async Task<User?> GetUserAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Users
            .AsNoTracking()
            .Include(u => u.Employee!).ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
