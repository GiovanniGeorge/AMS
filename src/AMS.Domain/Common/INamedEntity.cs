namespace AMS.Domain.Common;

/// <summary>
/// A lookup row whose whole identity is a name: suppliers, brands, locations,
/// departments, stores, units, asset statuses, items, categories.
/// It exists so one component can add any of them without knowing which it is.
/// </summary>
public interface INamedEntity
{
    string Name { get; set; }
}
