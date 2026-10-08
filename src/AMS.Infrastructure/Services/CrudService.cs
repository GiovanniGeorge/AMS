using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Domain.Common;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

/// <summary>
/// CreatedOn / CreatedById / ModifiedOn / ModifiedById are filled automatically
/// by ApplicationDbContext.SaveChangesAsync - not here.
///
/// One implementation that serves every simple master-data entity
/// (Category, Brand, Unit, Location, ...). It is registered once as an open generic,
/// so there is no need for 13 nearly identical service classes.
///
/// In Blazor Server the DbContext lives as long as the browser tab is open, so every
/// write method clears the change tracker when it finishes (also after an error).
/// Without that, editing a row right after adding or deactivating it fails with
/// "another instance with the same key is already being tracked", and a failed save
/// would be retried by every later save on the same tab.
/// </summary>
public class CrudService<T> : ICrudService<T> where T : BaseEntity
{
    private readonly ApplicationDbContext _db;

    public CrudService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<T>> GetAllAsync(bool includeInactive = false, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _db.Set<T>().AsNoTracking();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        if (!includeInactive)
        {
            query = query.Where(e => e.IsActive);
        }

        return await query.OrderByDescending(e => e.Id).ToListAsync();
    }

    public async Task<Paged<T>> GetPageAsync(int page = 1, bool includeInactive = false,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _db.Set<T>().AsNoTracking();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        if (!includeInactive)
        {
            query = query.Where(e => e.IsActive);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<T>.Size)
            .Take(Paged<T>.Size)
            .ToListAsync();

        return new Paged<T>(items, total);
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _db.Set<T>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task AddAsync(T entity)
    {
        entity.IsActive = true;

        try
        {
            _db.Set<T>().Add(entity);
            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Loads the row and copies the edited values onto it, so EF marks only the columns
    /// that really differ. _db.Update() would mark every column as changed, which filled
    /// the audit log with entries whose before and after were identical.
    /// </summary>
    public async Task UpdateAsync(T entity)
    {
        try
        {
            var current = await _db.Set<T>().FindAsync(entity.Id)
                          ?? throw new InvalidOperationException("This record no longer exists.");

            // Scalar columns only - navigation properties are not touched.
            _db.Entry(current).CurrentValues.SetValues(entity);
            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task DeactivateAsync(int id) => await SetActiveAsync(id, false);

    public async Task ActivateAsync(int id) => await SetActiveAsync(id, true);

    private async Task SetActiveAsync(int id, bool isActive)
    {
        try
        {
            var entity = await _db.Set<T>().FindAsync(id);
            if (entity is null)
            {
                return;
            }

            entity.IsActive = isActive;
            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }
}
