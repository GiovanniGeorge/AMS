using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Application.Common;

namespace AMS.Application.Transactions;

/// <summary>What the user fills in on the "Revaluation" screen.</summary>
public record RevaluationInput(
    int AssetId,
    decimal NewValue,
    DateTime Date,
    string? Reason);
