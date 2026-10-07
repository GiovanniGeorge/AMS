using Microsoft.EntityFrameworkCore;
using AMS.Domain.Common;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

/// <summary>
/// One asset can only be in one request at a time. Without this, the same asset could be
/// issued, transferred and scrapped at the same moment, and whichever request was approved
/// last would quietly overwrite the others.
/// </summary>
internal static class PendingRequests
{
    /// <summary>The ids of every asset that is already inside a request that is not finished.</summary>
    public static IQueryable<int> AssetIds(ApplicationDbContext db) =>
        db.AssetAssignments
            .Where(a => a.IsActive && a.Status == RequestStatus.Pending)
            .Select(a => a.AssetId)
        .Concat(db.TransferLines
            .Where(l => l.IsActive && l.Transfer!.IsActive
                        && (l.Transfer.Status == RequestStatus.Pending
                            || l.Transfer.Status == TransferService.InTransit))
            .Select(l => l.AssetId))
        .Concat(db.DisposalRequestLines
            .Where(l => l.IsActive && l.DisposalRequest!.IsActive
                        && l.DisposalRequest.Status == RequestStatus.Pending)
            .Select(l => l.AssetId));

    /// <summary>Throws when the asset is already waiting inside another kind of request.</summary>
    public static async Task EnsureFreeAsync(ApplicationDbContext db, int assetId)
    {
        if (await AssetIds(db).AnyAsync(id => id == assetId))
        {
            throw new InvalidOperationException(
                "This asset is already inside another request (issuance, transfer or disposal) that is not finished.");
        }
    }
}
