namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Service for password hashing and verification
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Hash a plain text password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against its hash
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hash">Password hash</param>
    /// <returns>True if password matches hash</returns>
    bool VerifyPassword(string password, string hash);
}