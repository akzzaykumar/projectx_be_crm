using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Users.Commands.ChangePassword;

/// <summary>
/// Handler for ChangePasswordCommand
/// </summary>
public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<ChangePasswordResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IApplicationDbContext context,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ChangePasswordResponse>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Changing password for UserId: {UserId}", request.UserId);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return Result<ChangePasswordResponse>.CreateFailure("User not found");
            }

            // Check if user uses external authentication (Google)
            if (user.IsExternalAuth)
            {
                _logger.LogWarning("User {UserId} uses external authentication and cannot change password", request.UserId);
                return Result<ChangePasswordResponse>.CreateFailure("Cannot change password for accounts using external authentication");
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Invalid current password for UserId: {UserId}", request.UserId);
                return Result<ChangePasswordResponse>.CreateFailure("Current password is incorrect");
            }

            // Hash new password
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Update password using domain method
            user.ChangePassword(newPasswordHash);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password changed successfully for UserId: {UserId}", request.UserId);

            return Result<ChangePasswordResponse>.CreateSuccess(new ChangePasswordResponse
            {
                Success = true,
                Message = "Password changed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for UserId: {UserId}", request.UserId);
            return Result<ChangePasswordResponse>.CreateFailure("An error occurred while changing password");
        }
    }
}
