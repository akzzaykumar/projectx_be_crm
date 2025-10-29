using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Handler for LogoutCommand
/// </summary>
public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<LogoutResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IApplicationDbContext context,
        ILogger<LogoutCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<LogoutResponse>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing logout request for refresh token");

        try
        {
            // Find user by refresh token
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Logout failed: Invalid or expired refresh token");
                return Result<LogoutResponse>.CreateFailure("Invalid or expired refresh token");
            }

            // Clear the refresh token (logout the user)
            user.ClearRefreshToken();

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User logged out successfully. UserId: {UserId}", user.Id);

            // Create response
            var response = new LogoutResponse
            {
                Message = "Successfully logged out",
                LoggedOutAt = DateTime.UtcNow
            };

            return Result<LogoutResponse>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during logout");
            return Result<LogoutResponse>.CreateFailure("An error occurred during logout");
        }
    }
}