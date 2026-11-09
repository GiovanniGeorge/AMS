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
