using FluentValidation;

namespace ActivoosCRM.Application.Features.Authentication.Commands.LoginUser;

/// <summary>
/// Validator for LoginUserCommand
/// </summary>
public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(1).WithMessage("Password cannot be empty");

        // RememberMe is optional and doesn't need validation
    }
}