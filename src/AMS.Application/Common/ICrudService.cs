using System.Linq.Expressions;
using AMS.Domain.Common;

namespace AMS.Application.Common;

/// <summary>
/// The contract every simple master-data screen uses.
/// It lives in the Application layer so the Blazor pages never touch EF Core directly.
/// Note: there is no hard Delete on purpose - AMS uses Deactivate (IsActive = false).
/// </summary>
public interface ICrudService<T> where T : BaseEntity
{
    Task<List<T>> GetAllAsync(bool includeInactive = false, params Expression<Func<T, object>>[] includes);

    /// <summary>One page of the same list, for screens that grow with use.</summary>
    Task<Paged<T>> GetPageAsync(int page = 1, bool includeInactive = false, params Expression<Func<T, object>>[] includes);

    Task<T?> GetByIdAsync(int id);

    Task AddAsync(T entity);

    Task UpdateAsync(T entity);

    /// <summary>Soft delete: sets IsActive = false. The row stays in the database.</summary>
    Task DeactivateAsync(int id);

    /// <summary>Brings a deactivated row back.</summary>
    Task ActivateAsync(int id);
}
