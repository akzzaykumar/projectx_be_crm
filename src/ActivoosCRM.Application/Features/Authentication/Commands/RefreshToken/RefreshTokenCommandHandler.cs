using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing token refresh request");

        try
        {
            // Find user by refresh token
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Token refresh failed: Invalid refresh token");
                return Result<RefreshTokenResponse>.CreateFailure("Invalid refresh token");
            }

            // Check if refresh token is expired
            if (user.RefreshTokenExpiry == null || user.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                _logger.LogWarning("Token refresh failed: Refresh token expired for user {UserId}", user.Id);

                // Clear expired refresh token
                user.ClearRefreshToken();
                await _context.SaveChangesAsync(cancellationToken);

                return Result<RefreshTokenResponse>.CreateFailure("Refresh token has expired");
            }

            // Check if user account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Token refresh failed: User account {UserId} is inactive", user.Id);
                return Result<RefreshTokenResponse>.CreateFailure("Account is inactive");
            }

            // Check if user account is locked
            if (user.IsLocked)
            {
                _logger.LogWarning("Token refresh failed: User account {UserId} is locked", user.Id);
                return Result<RefreshTokenResponse>.CreateFailure("Account is locked");
            }

            // Generate new tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user, request.RememberMe);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenExpiry = _jwtTokenService.GetRefreshTokenExpiry(request.RememberMe);

            // Update user with new refresh token (refresh token rotation for security)
            user.SetRefreshToken(newRefreshToken, refreshTokenExpiry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

            // Create response
            var response = new RefreshTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = _jwtTokenService.GetAccessTokenExpiry(request.RememberMe),
                TokenType = "Bearer",
                ExpiresIn = (int)_jwtTokenService.GetAccessTokenExpiry(request.RememberMe).Subtract(DateTime.UtcNow).TotalSeconds
            };

            return Result<RefreshTokenResponse>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while refreshing token");
            return Result<RefreshTokenResponse>.CreateFailure("An error occurred while refreshing the token");
        }
    }
}