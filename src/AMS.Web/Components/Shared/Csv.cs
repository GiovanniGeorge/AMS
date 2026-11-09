namespace AMS.Web.Components.Shared;

/// <summary>The CSV exports (asset register, employee custody, purchases report).</summary>
public static class Csv
{
    /// <summary>One cell. Quoted only when it holds a comma, a quote or a line break.</summary>
    public static string Cell(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    /// <summary>
    /// The finished file as a data: link, so no download endpoint is needed.
    /// It starts with a byte-order mark, without which Excel shows Arabic text as garbage.
    /// </summary>
    public static string Link(string text) =>
        "data:text/csv;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("\uFEFF" + text));
}
