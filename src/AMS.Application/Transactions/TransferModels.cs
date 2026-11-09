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
