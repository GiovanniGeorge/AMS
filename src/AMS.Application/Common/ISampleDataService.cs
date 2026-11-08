namespace AMS.Application.Common;

/// <summary>
/// A made-up dataset for trying the system out: categories, locations, people,
/// assets, one custody, a purchase order and an invoice.
///
/// It is invented, not a copy of anyone's register, and it contains no login
/// accounts - whoever installs AMS still creates their own on the setup page.
/// </summary>
public interface ISampleDataService
{
    /// <summary>True while the register is still empty, which is the only time it may be loaded.</summary>
    Task<bool> CanLoadAsync();

    /// <summary>Writes the dataset and returns a one-line summary of what it added.</summary>
    Task<string> LoadAsync();
}
