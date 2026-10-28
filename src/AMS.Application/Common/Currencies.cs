namespace AMS.Application.Common;

/// <summary>
/// The currencies AMS records amounts in. A short fixed list, not master data:
/// adding a currency is a decision, not something a user types into a box, and a
/// free-text field was producing "egp", "EGP " and "Egp" for the same thing.
/// </summary>
public static class Currencies
{
    public const string Default = "EGP";

    public static readonly IReadOnlyList<string> All = new[] { "EGP", "USD", "EUR" };

    /// <summary>
    /// Fits a stored value onto the list so old rows still show their own currency
    /// in the drop-down instead of appearing blank.
    /// </summary>
    public static string Normalise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Default;
        }

        var text = value.Trim();
        var known = All.FirstOrDefault(c => string.Equals(c, text, StringComparison.OrdinalIgnoreCase));
        return known ?? text;
    }
}
