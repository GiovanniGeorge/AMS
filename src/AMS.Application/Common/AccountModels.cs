namespace AMS.Application.Common;

/// <summary>The data put into the login cookie after a successful login.</summary>
public record LoginResult(int UserId, string Username, IReadOnlyList<string> Roles);
