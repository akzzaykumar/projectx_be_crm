namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthService
{
    Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(int userId, string email, string role);
    Task<(int UserId, string Email, string Role)?> ValidateTokenAsync(string token);
    Task<(string AccessToken, string RefreshToken)?> RefreshTokensAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(int userId);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
