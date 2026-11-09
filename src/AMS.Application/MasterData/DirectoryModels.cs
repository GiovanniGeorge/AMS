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
