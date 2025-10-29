using FluentValidation;

namespace ActivoosCRM.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Validator for ForgotPasswordCommand
/// </summary>
public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");
    }
}