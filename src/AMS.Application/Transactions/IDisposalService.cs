using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Application.Common;

namespace AMS.Application.Transactions;

/// <summary>What the user fills in on the "Disposal request" screen.</summary>
public record DisposalInput(
    int AssetId,
    string? DisposalReason,
    DateTime RequestDate,
    decimal? SaleProceeds,
    decimal? NetBookValue);

/// <summary>
/// Taking an asset out of service for good. The approver approves it; the asset then becomes Disposed.
/// The net book value is suggested by straight-line depreciation
/// (purchase cost, the category's useful life in years and salvage value) and can be edited.
/// Gain / loss = sale proceeds - net book value.
/// </summary>
public interface IDisposalService
{
    Task<Paged<DisposalRequest>> GetAllAsync(string? status = null, int page = 1);

    /// <summary>Requests waiting for the approver.</summary>
    Task<List<DisposalRequest>> GetPendingAsync();

    /// <summary>The logged-in user's own rejected requests, with the reason.</summary>
    Task<List<DisposalRequest>> GetMyRejectedAsync();

    /// <summary>Assets that can be disposed of: active, not already disposed, with no pending request.</summary>
    Task<List<Asset>> GetDisposableAssetsAsync();

    /// <summary>
    /// The suggested net book value: the latest stored depreciation entry if there is one,
    /// otherwise straight line from the purchase cost. Null when the asset has no purchase cost.
    /// </summary>
    Task<decimal?> SuggestNetBookValueAsync(int assetId, DateTime asOf);

    /// <summary><paramref name="incidentId"/> is filled in when the request came out of an incident.</summary>
    Task<int> CreateAsync(DisposalInput input, int? incidentId = null);

    /// <summary>Approver only. The asset becomes Disposed and its custody is closed.</summary>
    Task ApproveAsync(int disposalRequestId);

    /// <summary>Approver only. The reason is required; the asset does not change.</summary>
    Task RejectAsync(int disposalRequestId, string reason);
}
