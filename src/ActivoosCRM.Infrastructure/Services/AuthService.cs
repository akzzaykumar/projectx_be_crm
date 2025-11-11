using ActivoosCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using ActivoosCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Infrastructure.Services;

/// <summary>
/// Authentication service implementation - Will be fully implemented when User entity is added
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
        _jwtSecret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        _jwtIssuer = _configuration["Jwt:Issuer"] ?? "ActivoosCRM";
        _jwtAudience = _configuration["Jwt:Audience"] ?? "ActivoosCRM";
    }

    public async Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(Guid userId, string email, string role)
    {
        var accessToken = GenerateAccessToken(userId, email, role);
        var refreshToken = GenerateRefreshToken();
        
        // Find user and set refresh token
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(30); // 30 days default
            user.SetRefreshToken(refreshToken, refreshTokenExpiry);
            await _context.SaveChangesAsync(default);
        }
        
        return (accessToken, refreshToken);
    }

    private string GenerateAccessToken(Guid userId, string email, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<(Guid UserId, string Email, string Role)?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var emailClaim = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(emailClaim) || string.IsNullOrEmpty(roleClaim))
            {
                return null;
            }

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return null;
            }
            
            return (userId, emailClaim, roleClaim);
        }
        catch
        {
            return null;
        }
    }

    public async Task<(string AccessToken, string RefreshToken)?> RefreshTokensAsync(string refreshToken)
    {
        // Find user with matching refresh token
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && !u.IsDeleted);
            
        if (user == null)
            return null;
            
        // Check if refresh token is expired
        if (user.RefreshTokenExpiry == null || user.RefreshTokenExpiry.Value <= DateTime.UtcNow)
            return null;
            
        // Check if user is active
        if (!user.IsActive)
            return null;
            
        // Generate new tokens
        var newAccessToken = GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var newRefreshToken = GenerateRefreshToken();
        
        // Update user's refresh token
        var newRefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        user.SetRefreshToken(newRefreshToken, newRefreshTokenExpiry);
        await _context.SaveChangesAsync(default);
        
        return (newAccessToken, newRefreshToken);
    }

    public async Task RevokeRefreshTokenAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.ClearRefreshToken();
            await _context.SaveChangesAsync(default);
        }
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
