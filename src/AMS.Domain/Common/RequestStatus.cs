namespace AMS.Domain.Common;

/// <summary>
/// Status values for requests that need approval (stored in the existing string Status columns).
/// Kept as constants so the same text is used everywhere.
/// </summary>
public static class RequestStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
