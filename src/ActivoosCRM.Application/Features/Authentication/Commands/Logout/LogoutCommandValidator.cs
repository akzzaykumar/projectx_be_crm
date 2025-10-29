using FluentValidation;

namespace ActivoosCRM.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Validator for LogoutCommand
/// </summary>
public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required")
            .MinimumLength(10).WithMessage("Invalid refresh token format");
    }
}