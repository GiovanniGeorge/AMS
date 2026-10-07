namespace AMS.Application.Common;

/// <summary>Role names the code depends on. Create a role with exactly this name in the Roles screen.</summary>
public static class AppRoles
{
    /// <summary>
    /// The administrator: the one person who approves requests (issuance, transfer,
    /// incidents, disposal) and who posts depreciation. AMS has no other special role.
    /// </summary>
    public const string Admin = "Admin";
}
