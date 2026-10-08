using AMS.Application.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;

namespace AMS.Application.MasterData;

/// <summary>Filters for the Employees screen. Every filter is optional.</summary>
public record EmployeeFilter(
    string? Search = null,
    int? DepartmentId = null,
    bool IncludeInactive = false,
    int Page = 1);

/// <summary>
/// One custody period of one asset: who held it, from when to when.
/// Built from AssetAssignment, so it covers custody opened by an issuance and by a transfer.
/// </summary>
public record CustodyLine(
    int AssetId,
    string AssetNumber,
    string AssetName,
    string? Category,
    string? Status,
    LifecycleStage Stage,
    string? Location,
    DateTime? From,
    DateTime? To,
    bool IsCurrent);

/// <summary>
/// Searching the two people tables, plus the question the Employees screen exists
/// to answer: what is this person holding right now?
/// </summary>
public interface IDirectoryService
{
    Task<Paged<Employee>> SearchEmployeesAsync(EmployeeFilter filter);

    /// <summary>One employee with the department loaded.</summary>
    Task<Employee?> GetEmployeeAsync(int id);

    /// <summary>
    /// The assets the employee holds right now - read from Asset.CustodianId,
    /// which is the single source of truth for current custody.
    /// </summary>
    Task<List<Asset>> GetEmployeeAssetsAsync(int employeeId);

    /// <summary>Every approved custody this employee has ever had, newest first.</summary>
    Task<List<CustodyLine>> GetCustodyHistoryAsync(int employeeId);

    /// <summary>How many assets each of these employees holds right now, by employee id.</summary>
    Task<Dictionary<int, int>> CountAssetsAsync(IEnumerable<int> employeeIds);

    /// <summary>Login accounts, searched by username, with the linked employee loaded.</summary>
    Task<Paged<User>> SearchUsersAsync(string? search, bool includeInactive, int page);

    /// <summary>One login account with its linked employee.</summary>
    Task<User?> GetUserAsync(int id);

    /// <summary>
    /// The username of the active login already linked to this employee, or null when the
    /// person has none. <paramref name="exceptUserId"/> is the account being edited, so it
    /// is not reported as a clash with itself.
    /// </summary>
    Task<string?> LoginForEmployeeAsync(int employeeId, int exceptUserId);
}
