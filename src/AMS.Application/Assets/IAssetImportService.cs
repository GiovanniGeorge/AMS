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

/// <summary>
/// Registering many assets from a spreadsheet.
/// Reading and importing are separate on purpose: nothing is written until the
/// administrator has seen what the file contains and what was rejected.
/// </summary>
public interface IAssetImportService
{
    /// <summary>An empty workbook with the right columns and one example row.</summary>
    byte[] BuildTemplate();

    /// <summary>Reads and validates the file. Writes nothing.</summary>
    Task<ImportPreview> ReadAsync(Stream file, string fileName);

    /// <summary>
    /// Registers the valid rows. Lookups named in the sheet that do not exist yet
    /// (category, brand, supplier, location, department, condition) are created, and
    /// their names are returned so the administrator can see what the file added.
    /// </summary>
    Task<ImportResult> ImportAsync(IReadOnlyList<ImportRow> rows);

    /// <summary>The rejected rows as a workbook, with the reason in the last column.</summary>
    byte[] BuildRejectedWorkbook(IReadOnlyList<ImportRow> rows);
}
