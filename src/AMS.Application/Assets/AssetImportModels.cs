namespace AMS.Application.Assets;

/// <summary>
/// One row of an uploaded sheet, exactly as it was read, plus whatever is wrong with it.
/// Nothing is cleaned up silently: a row either passes or is handed back with the reason.
/// </summary>
public record ImportRow(
    int RowNumber,
    string? Name,
    string? Category,
    string? Condition,
    string? Brand,
    string? Model,
    string? SerialNumber,
    string? Supplier,
    string? Location,
    string? Department,
    string? CustodianEmployeeNumber,
    DateTime? PurchaseDate,
    decimal? PurchaseCost,
    string? Currency)
{
    /// <summary>Why this row cannot be registered. Empty means it can.</summary>
    public List<string> Problems { get; init; } = new();

    public bool IsValid => Problems.Count == 0;

    public string ProblemText => string.Join("; ", Problems);
}

/// <summary>What the sheet contains, before anything is written.</summary>
public record ImportPreview(List<ImportRow> Rows, string? FileProblem = null)
{
    public static ImportPreview Failed(string problem) => new(new List<ImportRow>(), problem);

    public int Valid => Rows.Count(r => r.IsValid);

    public int Rejected => Rows.Count - Valid;
}

/// <summary>What happened when the valid rows were written.</summary>
public record ImportResult(int Registered, List<string> CreatedLookups, List<ImportRow> Rejected);
