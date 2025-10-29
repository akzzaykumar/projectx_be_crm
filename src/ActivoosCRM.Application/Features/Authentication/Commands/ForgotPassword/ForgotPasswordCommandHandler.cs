using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace ActivoosCRM.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Handler for ForgotPasswordCommand
/// </summary>
public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<ForgotPasswordResponse>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing forgot password request for email: {Email}", request.Email);

        try
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

            // For security reasons, we always return success even if user doesn't exist
            // This prevents email enumeration attacks
            if (user == null)
            {
                _logger.LogWarning("Forgot password request for non-existent email: {Email}", request.Email);

                // Return success response but don't actually send email
                return Result<ForgotPasswordResponse>.CreateSuccess(CreateSuccessResponse(request.Email));
            }

            // Check if user account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Forgot password request for inactive user: {Email}", request.Email);

                // Return success response but don't actually send email for security
                return Result<ForgotPasswordResponse>.CreateSuccess(CreateSuccessResponse(request.Email));
            }

            // Generate password reset token
            var resetToken = GenerateSecureToken();
            var tokenExpiry = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

            // Set password reset token
            user.SetPasswordResetToken(resetToken, tokenExpiry);

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset token generated for user: {UserId}", user.Id);

            // Send password reset email
            var emailSent = await _emailService.SendPasswordResetEmailAsync(
                user.Email,
                resetToken,
                user.FullName);

            if (emailSent)
            {
                _logger.LogInformation("Password reset email sent successfully to: {Email}", user.Email);
            }
            else
            {
                _logger.LogWarning("Failed to send password reset email to: {Email}", user.Email);
                // Note: We still return success for security reasons (don't reveal email delivery issues)
            }

            // Create response
            var response = CreateSuccessResponse(request.Email); return Result<ForgotPasswordResponse>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during forgot password request for email: {Email}", request.Email);

            // For security, still return success response but log the error
            return Result<ForgotPasswordResponse>.CreateSuccess(CreateSuccessResponse(request.Email));
        }
    }

    /// <summary>
    /// Generates a cryptographically secure random token
    /// </summary>
    /// <returns>Base64 encoded secure token</returns>
    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32]; // 256-bit token
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    /// <summary>
    /// Creates a success response with masked email
    /// </summary>
    /// <param name="email">Original email address</param>
    /// <returns>Success response</returns>
    private static ForgotPasswordResponse CreateSuccessResponse(string email)
    {
        return new ForgotPasswordResponse
        {
            Message = "If an account with that email exists, we've sent password reset instructions to it.",
            RequestedAt = DateTime.UtcNow,
            MaskedEmail = MaskEmail(email)
        };
    }

    /// <summary>
    /// Masks email address for security (e.g., john@example.com -> j***@example.com)
    /// </summary>
    /// <param name="email">Email address to mask</param>
    /// <returns>Masked email address</returns>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***@***.***";

        var parts = email.Split('@');
        if (parts.Length != 2)
            return "***@***.***";

        var localPart = parts[0];
        var domainPart = parts[1];

        // Mask local part
        var maskedLocal = localPart.Length switch
        {
            1 => "*",
            2 => localPart[0] + "*",
            _ => localPart[0] + new string('*', localPart.Length - 2) + localPart[^1]
        };

        return $"{maskedLocal}@{domainPart}";
    }
}