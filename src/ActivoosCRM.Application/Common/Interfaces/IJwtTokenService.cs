using ActivoosCRM.Domain.Entities;

namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Interface for JWT token management
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates an access token for the user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="rememberMe">Whether to use extended expiry</param>
    /// <returns>JWT access token</returns>
    string GenerateAccessToken(User user, bool rememberMe = false);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Gets the access token expiry time
    /// </summary>
    /// <param name="rememberMe">Whether to use extended expiry</param>
    /// <returns>Expiry date time</returns>
    DateTime GetAccessTokenExpiry(bool rememberMe = false);

    /// <summary>
    /// Gets the refresh token expiry time
    /// </summary>
    /// <param name="rememberMe">Whether to use extended expiry</param>
    /// <returns>Expiry date time</returns>
    DateTime GetRefreshTokenExpiry(bool rememberMe = false);

    /// <summary>
    /// Validates and extracts user ID from JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID if valid, null otherwise</returns>
    Guid? ValidateTokenAndGetUserId(string token);
}