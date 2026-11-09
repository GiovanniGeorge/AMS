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
