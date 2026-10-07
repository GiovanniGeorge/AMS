using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Application.Common;

namespace AMS.Application.Transactions;

/// <summary>What the user fills in on the "Return asset" screen.</summary>
public record ReturnInput(
    int AssetId,
    int EmployeeId,
    int StoreId,
    DateTime Date,
    string? Condition,
    string? Reason);

/// <summary>
/// An asset coming back from an employee into a store. There is no approval:
/// the asset becomes In store, its custodian is cleared and the open custody is closed
/// with the return date, all at once.
/// </summary>
public interface IReturnService
{
    Task<Paged<Return>> GetAllAsync(int page = 1);

    /// <summary>Assets that can be returned: active and Assigned.</summary>
    Task<List<Asset>> GetReturnableAssetsAsync();

    Task<int> CreateAsync(ReturnInput input);
}
