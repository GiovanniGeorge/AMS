using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Application.Common;

namespace AMS.Application.Transactions;

/// <summary>What the user fills in on the "Transfer asset" screen. "From" is taken from the asset itself.</summary>
public record TransferInput(
    int AssetId,
    int? ToCustodianId,
    int? ToDepartmentId,
    int? ToLocationId,
    DateTime Date);

/// <summary>
/// Moving an asset from one custodian / department / location to another, in two steps:
/// the approver approves (the asset becomes In transit and the dispatch date is stamped),
/// then the receiving side confirms receipt (the asset takes the new custodian, department and location).
/// </summary>
public interface ITransferService
{
    /// <summary>All transfers, newest first. <paramref name="status"/> null = all.</summary>
    Task<Paged<Transfer>> GetAllAsync(string? status = null, int page = 1);

    /// <summary>Transfers waiting for the approver.</summary>
    Task<List<Transfer>> GetPendingAsync();

    /// <summary>The logged-in user's own rejected transfers, with the rejection reason.</summary>
    Task<List<Transfer>> GetMyRejectedAsync();

    /// <summary>Assets that can be transferred: active, In store or Assigned, with no transfer in progress.</summary>
    Task<List<Asset>> GetTransferableAssetsAsync();

    Task<int> CreateAsync(TransferInput input);

    /// <summary>Approver only. Stamps the dispatch date; the asset becomes In transit.</summary>
    Task ApproveAsync(int transferId);

    /// <summary>Approver only. The reason is required; the asset does not change.</summary>
    Task RejectAsync(int transferId, string reason);

    /// <summary>
    /// The second step: the asset has arrived. It takes the new custodian, department and location,
    /// a new custody period is opened and the old one is closed.
    /// </summary>
    Task ConfirmReceiptAsync(int transferId);
}
