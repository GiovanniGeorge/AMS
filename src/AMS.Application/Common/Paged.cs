namespace AMS.Application.Common;

/// <summary>
/// One page of a list plus how many rows match in total.
/// Every screen that grows with use returns this instead of the whole table.
/// </summary>
public record Paged<T>(List<T> Items, int Total)
{
    /// <summary>Rows per page everywhere in AMS.</summary>
    public const int Size = 50;

    public static Paged<T> Empty => new(new List<T>(), 0);
}
