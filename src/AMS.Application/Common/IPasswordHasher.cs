namespace AMS.Application.Common;

/// <summary>Hashes and verifies user passwords. A plain password is never stored.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>True when <paramref name="password"/> matches the stored <paramref name="passwordHash"/>.</summary>
    bool Verify(string passwordHash, string password);
}
