using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Application.MasterData;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class DirectoryService : IDirectoryService
{
    private readonly ApplicationDbContext _db;

    public DirectoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ------------------------------------------------------------------
    // Employees
    // ------------------------------------------------------------------

    public async Task<Paged<Employee>> SearchEmployeesAsync(EmployeeFilter filter)
    {
        IQueryable<Employee> query = _db.Employees
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

    public async Task<Employee?> GetEmployeeAsync(int id)
    {
        return await _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Asset>> GetEmployeeAssetsAsync(int employeeId)
    {
        return await _db.Assets
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Status)
            .Include(a => a.Location)
            .Include(a => a.Department)
            .Where(a => a.IsActive && a.CustodianId == employeeId)
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    public async Task<List<CustodyLine>> GetCustodyHistoryAsync(int employeeId)
    {
        // No Include here on purpose: the Select below decides what is read,
        // and EF would only warn that the Includes were ignored.
        return await _db.AssetAssignments
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

    public async Task<Dictionary<int, int>> CountAssetsAsync(IEnumerable<int> employeeIds)
    {
        var ids = employeeIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        // Grouped in the database, one round trip for the whole page.
        var counts = await _db.Assets
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

    public async Task<Paged<User>> SearchUsersAsync(string? search, bool includeInactive, int page)
    {
        IQueryable<User> query = _db.Users
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

    public async Task<string?> LoginForEmployeeAsync(int employeeId, int exceptUserId)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.EmployeeId == employeeId && u.Id != exceptUserId)
            .Select(u => u.Username)
            .FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserAsync(int id)
    {
        return await _db.Users
            .AsNoTracking()
            .Include(u => u.Employee!).ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
