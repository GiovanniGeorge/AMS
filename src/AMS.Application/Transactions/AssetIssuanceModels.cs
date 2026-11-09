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
