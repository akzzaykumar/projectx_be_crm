using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ActivoosCRM.Infrastructure.Services;

/// <summary>
/// JWT token service implementation
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        _issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        _audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
    }

    /// <summary>
    /// Generates an access token for the user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="rememberMe">Whether to use extended expiry</param>
    /// <returns>JWT access token</returns>
    public string GenerateAccessToken(User user, bool rememberMe = false)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("user_id", user.Id.ToString()),
            new("email", user.Email),
            new("role", user.Role.ToString()),
            new("is_email_verified", user.IsEmailVerified.ToString().ToLower())
        };

        if (!string.IsNullOrEmpty(user.PhoneNumber))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
            claims.Add(new Claim("phone_number", user.PhoneNumber));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = GetAccessTokenExpiry(rememberMe), // Use rememberMe parameter for expiry
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Gets the access token expiry time
    /// </summary>
    /// <param name="rememberMe">Whether to use extended expiry</param>
    /// <returns>Expiry date time</returns>
    public DateTime GetAccessTokenExpiry(bool rememberMe = false)
    {
        var expiryMinutes = rememberMe
            ? int.Parse(_configuration["Jwt:AccessTokenExpiryMinutesExtended"] ?? "1440") // 24 hours
            : int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60"); // 1 hour

        return DateTime.UtcNow.AddMinutes(expiryMinutes);
    }

    /// <summary>
    /// Gets the refresh token expiry time
    /// </summary>
    /// <param name="rememberMe">Whether to use extended expiry</param>
    /// <returns>Expiry date time</returns>
    public DateTime GetRefreshTokenExpiry(bool rememberMe = false)
    {
        var expiryDays = rememberMe
            ? int.Parse(_configuration["Jwt:RefreshTokenExpiryDaysExtended"] ?? "90") // 90 days
            : int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "30"); // 30 days

        return DateTime.UtcNow.AddDays(expiryDays);
    }

    /// <summary>
    /// Validates and extracts user ID from JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID if valid, null otherwise</returns>
    public Guid? ValidateTokenAndGetUserId(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken &&
                jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                 principal.FindFirst("user_id")?.Value;

                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}