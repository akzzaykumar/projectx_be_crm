using ActivoosCRM.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Authentication.Commands.VerifyEmail;

/// <summary>
/// Command to verify user email address
/// </summary>
public record VerifyEmailCommand : IRequest<VerifyEmailResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}

/// <summary>
/// Response for verify email command
/// </summary>
public record VerifyEmailResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Validator for verify email command
/// </summary>
public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email format is invalid");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required")
            .MinimumLength(6).WithMessage("Invalid verification token");
    }
}

/// <summary>
/// Handler for verify email command
/// </summary>
public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>
{
    private readonly IApplicationDbContext _context;

    public VerifyEmailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VerifyEmailResponse> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            return new VerifyEmailResponse
            {
                Success = false,
                Message = "User not found"
            };
        }

        // Check if email is already verified
        if (user.IsEmailVerified)
        {
            return new VerifyEmailResponse
            {
                Success = false,
                Message = "Email is already verified"
            };
        }

        // Validate verification token
        if (!user.IsEmailVerificationTokenValid(request.Token))
        {
            return new VerifyEmailResponse
            {
                Success = false,
                Message = "Invalid or expired verification code"
            };
        }

        // Verify email successfully
        user.VerifyEmail();

        // Save changes
        await _context.SaveChangesAsync(cancellationToken);

        return new VerifyEmailResponse
        {
            Success = true,
            Message = "Email verified successfully! Your account is now active."
        };
    }
}