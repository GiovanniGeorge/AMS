using Microsoft.EntityFrameworkCore;
using AMS.Application.Audit;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _db;

    public AuditLogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AuditPage> SearchAsync(AuditFilter filter)
    {
        var query = _db.AuditLogs.AsNoTracking().Include(a => a.User).AsQueryable();

        if (filter.Action is not null)
        {
            query = query.Where(a => a.Action == filter.Action);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
        {
            query = query.Where(a => a.EntityName == filter.EntityName);
        }

        if (filter.UserId is not null)
        {
            query = query.Where(a => a.UserId == filter.UserId);
        }

        if (filter.From is not null)
        {
            query = query.Where(a => a.DateTime >= filter.From);
        }

        if (filter.To is not null)
        {
            var to = filter.To.Value.Date.AddDays(1);
            query = query.Where(a => a.DateTime < to);
        }

        var total = await query.CountAsync();

        var page = filter.Page < 1 ? 1 : filter.Page;
        var size = filter.PageSize < 1 ? 50 : filter.PageSize;

        var items = await query
            .OrderByDescending(a => a.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return new AuditPage(items, total);
    }

    public async Task<List<string>> GetEntityNamesAsync()
    {
        return await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityName != null)
            .Select(a => a.EntityName!)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
    }
}
