using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using BCrypt.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Authentication.Commands.ResetPassword;

/// <summary>
/// Handler for ResetPasswordCommand
/// </summary>
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ResetPasswordResponse>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing password reset for token: {Token}", request.ResetToken);

        try
        {
            // Find user by reset token
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PasswordResetToken == request.ResetToken, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Password reset attempted with invalid token: {Token}", request.ResetToken);
                return Result<ResetPasswordResponse>.CreateFailure("Invalid or expired reset token");
            }

            // Check if token has expired
            if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Password reset attempted with expired token for user: {UserId}", user.Id);

                // Clear the expired token
                user.ClearPasswordResetToken();
                await _context.SaveChangesAsync(cancellationToken);

                return Result<ResetPasswordResponse>.CreateFailure("Reset token has expired. Please request a new password reset");
            }

            // Check if user account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Password reset attempted for inactive user: {UserId}", user.Id);
                return Result<ResetPasswordResponse>.CreateFailure("User account is not active");
            }

            // Hash the new password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Update user password and clear reset token
            user.ChangePassword(hashedPassword);
            user.ClearRefreshToken(); // Invalidate all existing sessions

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset successful for user: {UserId}", user.Id);

            // Create success response
            var response = new ResetPasswordResponse
            {
                Message = "Your password has been reset successfully. You can now log in with your new password.",
                ResetAt = DateTime.UtcNow,
                Success = true
            };

            return Result<ResetPasswordResponse>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password reset for token: {Token}", request.ResetToken);
            return Result<ResetPasswordResponse>.CreateFailure("An error occurred while resetting your password. Please try again.");
        }
    }
}