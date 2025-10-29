using ActivoosCRM.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ActivoosCRM.Application.Features.Authentication.Commands.ResendVerificationEmail;

/// <summary>
/// Command to resend email verification
/// </summary>
public record ResendVerificationEmailCommand : IRequest<ResendVerificationEmailResponse>
{
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Response for resend verification email command
/// </summary>
public record ResendVerificationEmailResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Validator for resend verification email command
/// </summary>
public class ResendVerificationEmailCommandValidator : AbstractValidator<ResendVerificationEmailCommand>
{
    public ResendVerificationEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email format is invalid");
    }
}

/// <summary>
/// Handler for resend verification email command
/// </summary>
public class ResendVerificationEmailCommandHandler : IRequestHandler<ResendVerificationEmailCommand, ResendVerificationEmailResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public ResendVerificationEmailCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<ResendVerificationEmailResponse> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            // Don't reveal if user exists or not for security
            return new ResendVerificationEmailResponse
            {
                Success = true,
                Message = "If the email exists, a verification code has been sent"
            };
        }

        // Check if email is already verified
        if (user.IsEmailVerified)
        {
            return new ResendVerificationEmailResponse
            {
                Success = false,
                Message = "Email is already verified"
            };
        }

        // Basic rate limiting - check if last resend was less than 1 minute ago
        if (user.LastEmailResendAt.HasValue &&
            DateTime.UtcNow.Subtract(user.LastEmailResendAt.Value).TotalMinutes < 1)
        {
            var remainingTime = 60 - (int)DateTime.UtcNow.Subtract(user.LastEmailResendAt.Value).TotalSeconds;
            return new ResendVerificationEmailResponse
            {
                Success = false,
                Message = $"Please wait {remainingTime} seconds before requesting another verification code"
            };
        }

        // Generate new verification token (6-digit OTP)
        var verificationToken = GenerateVerificationToken();
        var expiry = DateTime.UtcNow.AddHours(24); // Token valid for 24 hours

        // Set verification token
        user.SetEmailVerificationToken(verificationToken, expiry);

        // Save changes
        await _context.SaveChangesAsync(cancellationToken);

        // Send verification email
        try
        {
            await _emailService.SendEmailVerificationAsync(
                user.Email,
                verificationToken,
                user.FirstName);
        }
        catch (Exception)
        {
            // Log error but don't fail the request
            // In production, you would log this error
        }

        return new ResendVerificationEmailResponse
        {
            Success = true,
            Message = "Verification code has been sent to your email"
        };
    }

    private static string GenerateVerificationToken()
    {
        // Generate a 6-digit numeric code
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var code = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
        return code.ToString("D6");
    }
}