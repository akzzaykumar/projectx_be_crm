using FluentValidation;

namespace ActivoosCRM.Application.Features.Users.Commands.UpdateUserProfile;

/// <summary>
/// Validator for UpdateUserProfileCommand
/// </summary>
public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Phone number must be in valid international format");

        When(x => x.CustomerProfile != null, () =>
        {
            RuleFor(x => x.CustomerProfile!.Gender)
                .Must(g => g == null || new[] { "Male", "Female", "Other", "PreferNotToSay" }.Contains(g))
                .WithMessage("Gender must be Male, Female, Other, or PreferNotToSay");

            RuleFor(x => x.CustomerProfile!.DateOfBirth)
                .LessThan(DateTime.Today)
                .When(x => x.CustomerProfile!.DateOfBirth.HasValue)
                .WithMessage("Date of birth must be in the past");

            RuleFor(x => x.CustomerProfile!.EmergencyContactPhone)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .When(x => !string.IsNullOrEmpty(x.CustomerProfile!.EmergencyContactPhone))
                .WithMessage("Emergency contact phone must be in valid international format");

            RuleFor(x => x.CustomerProfile!.PreferredLanguage)
                .MaximumLength(50)
                .When(x => !string.IsNullOrEmpty(x.CustomerProfile!.PreferredLanguage))
                .WithMessage("Preferred language must not exceed 50 characters");
        });
    }
}
