using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Application.Common;

namespace AMS.Application.Transactions;

/// <summary>What the user fills in on the "Issue asset" screen.</summary>
public record IssuanceRequest(
    int AssetId,
    int StoreId,
    int EmployeeId,
    int? DepartmentId,
    int? LocationId,
    DateTime Date,
    string? Notes);

/// <summary>
/// Asset issuance: one request creates an Issue (asset leaves the store) and an
/// AssetAssignment (the custody), both Pending. The approver approves or rejects the assignment.
/// </summary>
public interface IAssetIssuanceService
{
    /// <summary>All issuance requests, newest first. <paramref name="status"/> null = all.</summary>
    Task<Paged<AssetAssignment>> GetAllAsync(string? status = null, int page = 1);

    /// <summary>Requests waiting for the approver.</summary>
    Task<List<AssetAssignment>> GetPendingAsync();

    /// <summary>The logged-in user's own rejected requests, with the rejection reason.</summary>
    Task<List<AssetAssignment>> GetMyRejectedAsync();

    /// <summary>Assets that can be issued: active, In store, and without a pending request.</summary>
    Task<List<Asset>> GetIssuableAssetsAsync();

    /// <summary>Creates the Issue + AssetAssignment pair. Returns the assignment Id.</summary>
    Task<int> CreateAsync(IssuanceRequest request);

    /// <summary>Approver only. The asset takes the custodian, department and location and becomes Assigned.</summary>
    Task ApproveAsync(int assignmentId);

    /// <summary>Approver only. The reason is required; the asset does not change.</summary>
    Task RejectAsync(int assignmentId, string reason);
}
