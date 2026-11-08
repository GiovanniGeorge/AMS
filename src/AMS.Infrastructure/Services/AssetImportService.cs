using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using AMS.Application.Assets;
using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class AssetImportService : IAssetImportService
{
    /// <summary>The columns, in order. The header row must match these names.</summary>
    private static readonly string[] Columns =
    {
        "Name", "Category", "Condition", "Brand", "Model", "Serial number",
        "Supplier", "Location", "Department", "Custodian employee number",
        "Purchase date", "Purchase cost", "Currency"
    };

    private readonly ApplicationDbContext _db;
    private readonly IAssetService _assets;

    public AssetImportService(ApplicationDbContext db, IAssetService assets)
    {
        _db = db;
        _assets = assets;
    }

    // ------------------------------------------------------------------
    // Template
    // ------------------------------------------------------------------

    public byte[] BuildTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Assets");

        for (var i = 0; i < Columns.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = Columns[i];
        }

        sheet.Row(1).Style.Font.Bold = true;

        // One example row, so the shape of each column is obvious.
        sheet.Cell(2, 1).Value = "Dell Latitude 5440";
        sheet.Cell(2, 2).Value = "Laptops";
        sheet.Cell(2, 3).Value = "New";
        sheet.Cell(2, 4).Value = "Dell";
        sheet.Cell(2, 5).Value = "5440";
        sheet.Cell(2, 6).Value = "SN-123456";
        sheet.Cell(2, 7).Value = "TechnoSupply";
        sheet.Cell(2, 8).Value = "Head office";
        sheet.Cell(2, 9).Value = "IT";
        sheet.Cell(2, 10).Value = "";
        sheet.Cell(2, 11).Value = DateTime.Today;
        sheet.Cell(2, 11).Style.DateFormat.Format = "yyyy-mm-dd";
        sheet.Cell(2, 12).Value = 18500;
        sheet.Cell(2, 13).Value = "EGP";

        var notes = workbook.AddWorksheet("How to use");
        notes.Cell(1, 1).Value = "Filling in the Assets sheet";
        notes.Cell(1, 1).Style.Font.Bold = true;
        var lines = new[]
        {
            "Delete the example row before you upload.",
            "Name, Category and Condition are required. Everything else may be left empty.",
            "Asset numbers are NOT in this file: the system generates them from the category.",
            "Category, Brand, Supplier, Location, Department and Condition are matched by name,",
            "   and anything that does not exist yet is created for you.",
            "Custodian is matched on the EMPLOYEE NUMBER, and the employee must already exist.",
            "   Leave it empty and the asset is registered In store.",
            "Purchase date must be a real date cell, not text.",
            "Currency must be EGP, USD or EUR."
        };
        for (var i = 0; i < lines.Length; i++)
        {
            notes.Cell(i + 3, 1).Value = lines[i];
        }
        notes.Column(1).Width = 95;

        sheet.Columns().AdjustToContents();
        return Save(workbook);
    }

    // ------------------------------------------------------------------
    // Reading
    // ------------------------------------------------------------------

    public async Task<ImportPreview> ReadAsync(Stream file, string fileName)
    {
        if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return ImportPreview.Failed("Upload an .xlsx file. Save the sheet as Excel Workbook first.");
        }

        List<ImportRow> rows;

        try
        {
            using var workbook = new XLWorkbook(file);
            var sheet = workbook.Worksheets.FirstOrDefault()
                        ?? throw new InvalidOperationException("The workbook has no sheets.");

            var header = sheet.Row(1);
            if (!string.Equals(Text(header.Cell(1)), "Name", StringComparison.OrdinalIgnoreCase))
            {
                return ImportPreview.Failed(
                    "The first sheet does not look like the template: cell A1 should be \"Name\". "
                    + "Download the template and fill that in.");
            }

            rows = sheet.RowsUsed()
                .Skip(1)
                .Select(ReadRow)
                .Where(r => !IsBlank(r))
                .ToList();
        }
        catch (Exception ex)
        {
            return ImportPreview.Failed("The file could not be read: " + ex.Message);
        }

        if (rows.Count == 0)
        {
            return ImportPreview.Failed("The sheet has no rows under the header.");
        }

        await ValidateAsync(rows);
        return new ImportPreview(rows);
    }

    private static ImportRow ReadRow(IXLRow row) => new(
        row.RowNumber(),
        Text(row.Cell(1)),
        Text(row.Cell(2)),
        Text(row.Cell(3)),
        Text(row.Cell(4)),
        Text(row.Cell(5)),
        Text(row.Cell(6)),
        Text(row.Cell(7)),
        Text(row.Cell(8)),
        Text(row.Cell(9)),
        Text(row.Cell(10)),
        Date(row.Cell(11)),
        Number(row.Cell(12)),
        Text(row.Cell(13)));

    private static bool IsBlank(ImportRow r) =>
        string.IsNullOrWhiteSpace(r.Name)
        && string.IsNullOrWhiteSpace(r.Category)
        && string.IsNullOrWhiteSpace(r.Condition)
        && string.IsNullOrWhiteSpace(r.SerialNumber);

    /// <summary>
    /// Everything that can be checked before writing: the three required columns, the
    /// currency, serial numbers repeated inside the file or already in the register, and
    /// a custodian that does not exist.
    /// </summary>
    private async Task ValidateAsync(List<ImportRow> rows)
    {
        var serials = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.SerialNumber))
            .Select(r => r.SerialNumber!.Trim())
            .ToList();

        var takenSerials = serials.Count == 0
            ? new List<string>()
            : await _db.Assets
                .Where(a => a.SerialNumber != null && serials.Contains(a.SerialNumber))
                .Select(a => a.SerialNumber!)
                .ToListAsync();

        var employeeNumbers = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.CustodianEmployeeNumber))
            .Select(r => r.CustodianEmployeeNumber!.Trim())
            .ToList();

        var knownEmployees = employeeNumbers.Count == 0
            ? new List<string>()
            : await _db.Employees
                .Where(e => e.IsActive && employeeNumbers.Contains(e.EmployeeNumber))
                .Select(e => e.EmployeeNumber)
                .ToListAsync();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Name)) row.Problems.Add("Name is missing");
            if (string.IsNullOrWhiteSpace(row.Category)) row.Problems.Add("Category is missing");
            if (string.IsNullOrWhiteSpace(row.Condition)) row.Problems.Add("Condition is missing");

            if (!string.IsNullOrWhiteSpace(row.Currency)
                && !AMS.Application.Common.Currencies.All.Contains(row.Currency.Trim().ToUpperInvariant()))
            {
                row.Problems.Add($"Currency \"{row.Currency}\" is not EGP, USD or EUR");
            }

            if (row.PurchaseCost is < 0) row.Problems.Add("Purchase cost is negative");

            if (!string.IsNullOrWhiteSpace(row.SerialNumber))
            {
                var serial = row.SerialNumber.Trim();

                if (takenSerials.Contains(serial))
                {
                    row.Problems.Add($"Serial number \"{serial}\" is already registered");
                }

                if (serials.Count(s => string.Equals(s, serial, StringComparison.OrdinalIgnoreCase)) > 1)
                {
                    row.Problems.Add($"Serial number \"{serial}\" appears more than once in this file");
                }
            }

            if (!string.IsNullOrWhiteSpace(row.CustodianEmployeeNumber)
                && !knownEmployees.Contains(row.CustodianEmployeeNumber.Trim()))
            {
                row.Problems.Add($"No employee has the number \"{row.CustodianEmployeeNumber}\"");
            }
        }
    }

    // ------------------------------------------------------------------
    // Importing
    // ------------------------------------------------------------------

    public async Task<ImportResult> ImportAsync(IReadOnlyList<ImportRow> rows)
    {
        var valid = rows.Where(r => r.IsValid).ToList();
        var rejected = rows.Where(r => !r.IsValid).ToList();
        var created = new List<string>();

        var categories = await LookupAsync<Category>();
        var statuses = await LookupAsync<AssetStatus>();
        var brands = await LookupAsync<Brand>();
        var suppliers = await LookupAsync<Supplier>();
        var locations = await LookupAsync<Location>();
        var departments = await LookupAsync<Department>();

        var employees = await _db.Employees
            .AsNoTracking()
            .Where(e => e.IsActive)
            .ToDictionaryAsync(e => e.EmployeeNumber.Trim(), e => e.Id);

        var registered = 0;

        foreach (var row in valid)
        {
            try
            {
                var asset = new Asset
                {
                    Name = row.Name!.Trim(),
                    CategoryId = await EnsureAsync<Category>(categories, row.Category!, created, "Category"),
                    StatusId = await EnsureAsync<AssetStatus>(statuses, row.Condition!, created, "Condition"),
                    Model = Clean(row.Model),
                    SerialNumber = Clean(row.SerialNumber),
                    PurchaseDate = row.PurchaseDate,
                    PurchaseCost = row.PurchaseCost,
                    Currency = string.IsNullOrWhiteSpace(row.Currency)
                        ? AMS.Application.Common.Currencies.Default
                        : row.Currency.Trim().ToUpperInvariant()
                };

                if (!string.IsNullOrWhiteSpace(row.Brand))
                    asset.BrandId = await EnsureAsync<Brand>(brands, row.Brand, created, "Brand");

                if (!string.IsNullOrWhiteSpace(row.Supplier))
                    asset.SupplierId = await EnsureAsync<Supplier>(suppliers, row.Supplier, created, "Supplier");

                if (!string.IsNullOrWhiteSpace(row.Location))
                    asset.LocationId = await EnsureAsync<Location>(locations, row.Location, created, "Location");

                if (!string.IsNullOrWhiteSpace(row.Department))
                    asset.DepartmentId = await EnsureAsync<Department>(departments, row.Department, created, "Department");

                if (!string.IsNullOrWhiteSpace(row.CustodianEmployeeNumber)
                    && employees.TryGetValue(row.CustodianEmployeeNumber.Trim(), out var employeeId))
                {
                    asset.CustodianId = employeeId;
                }

                // The same RegisterAsync every asset goes through: numbering and the
                // opening lifecycle stage are decided there, not here.
                await _assets.RegisterAsync(asset);
                registered++;
            }
            catch (Exception ex)
            {
                row.Problems.Add(ex.InnerException?.Message ?? ex.Message);
                rejected.Add(row);
            }
        }

        return new ImportResult(registered, created.Distinct().ToList(), rejected);
    }

    private async Task<Dictionary<string, int>> LookupAsync<T>()
        where T : Domain.Common.BaseEntity, Domain.Common.INamedEntity
    {
        var rows = await _db.Set<T>().AsNoTracking().ToListAsync();
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            map.TryAdd(row.Name.Trim(), row.Id);
        }

        return map;
    }

    /// <summary>The id of the lookup with this name, creating it when the sheet names a new one.</summary>
    private async Task<int> EnsureAsync<T>(
        Dictionary<string, int> known, string name, List<string> created, string what)
        where T : Domain.Common.BaseEntity, Domain.Common.INamedEntity, new()
    {
        var clean = name.Trim();

        if (known.TryGetValue(clean, out var existing))
        {
            return existing;
        }

        var row = new T { Name = clean };

        try
        {
            _db.Set<T>().Add(row);
            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }

        known[clean] = row.Id;
        created.Add($"{what}: {clean}");
        return row.Id;
    }

    // ------------------------------------------------------------------
    // Rejected rows back out
    // ------------------------------------------------------------------

    public byte[] BuildRejectedWorkbook(IReadOnlyList<ImportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Rejected");

        for (var i = 0; i < Columns.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = Columns[i];
        }

        sheet.Cell(1, Columns.Length + 1).Value = "What is missing";
        sheet.Row(1).Style.Font.Bold = true;

        var line = 2;
        foreach (var row in rows)
        {
            sheet.Cell(line, 1).Value = row.Name;
            sheet.Cell(line, 2).Value = row.Category;
            sheet.Cell(line, 3).Value = row.Condition;
            sheet.Cell(line, 4).Value = row.Brand;
            sheet.Cell(line, 5).Value = row.Model;
            sheet.Cell(line, 6).Value = row.SerialNumber;
            sheet.Cell(line, 7).Value = row.Supplier;
            sheet.Cell(line, 8).Value = row.Location;
            sheet.Cell(line, 9).Value = row.Department;
            sheet.Cell(line, 10).Value = row.CustodianEmployeeNumber;
            if (row.PurchaseDate is { } date)
            {
                sheet.Cell(line, 11).Value = date;
                sheet.Cell(line, 11).Style.DateFormat.Format = "yyyy-mm-dd";
            }
            if (row.PurchaseCost is { } cost) sheet.Cell(line, 12).Value = cost;
            sheet.Cell(line, 13).Value = row.Currency;
            sheet.Cell(line, Columns.Length + 1).Value = row.ProblemText;
            line++;
        }

        sheet.Columns().AdjustToContents();
        return Save(workbook);
    }

    // ------------------------------------------------------------------
    // Cell helpers
    // ------------------------------------------------------------------

    private static byte[] Save(XLWorkbook workbook)
    {
        using var memory = new MemoryStream();
        workbook.SaveAs(memory);
        return memory.ToArray();
    }

    private static string? Text(IXLCell cell)
    {
        var text = cell.GetFormattedString()?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static DateTime? Date(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
        return DateTime.TryParse(cell.GetFormattedString(), out var parsed) ? parsed : null;
    }

    private static decimal? Number(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
        return decimal.TryParse(cell.GetFormattedString(), out var parsed) ? parsed : null;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
