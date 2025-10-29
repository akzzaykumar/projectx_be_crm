using FluentValidation;

namespace ActivoosCRM.Application.Features.Providers.Commands.UpdateProvider;

/// <summary>
/// Validator for UpdateProviderCommand
/// </summary>
public class UpdateProviderCommandValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderCommandValidator()
    {
        RuleFor(x => x.ProviderId)
            .NotEmpty()
            .WithMessage("Provider ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.BusinessName)
            .NotEmpty()
            .WithMessage("Business name is required")
            .MaximumLength(200)
            .WithMessage("Business name cannot exceed 200 characters");

        RuleFor(x => x.BusinessEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.BusinessEmail))
            .WithMessage("Business email must be a valid email address");

        RuleFor(x => x.BusinessPhone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.BusinessPhone))
            .WithMessage("Business phone cannot exceed 20 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description))
            .WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.Website)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Website))
            .WithMessage("Website URL cannot exceed 500 characters")
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.Website))
            .WithMessage("Website must be a valid URL");

        RuleFor(x => x.AddressLine1)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.AddressLine1))
            .WithMessage("Address line 1 cannot exceed 200 characters");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.AddressLine2))
            .WithMessage("Address line 2 cannot exceed 200 characters");

        RuleFor(x => x.City)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.City))
            .WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.StateProvince)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.StateProvince))
            .WithMessage("State/Province cannot exceed 100 characters");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.PostalCode))
            .WithMessage("Postal code cannot exceed 20 characters");

        RuleFor(x => x.Country)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Country))
            .WithMessage("Country cannot exceed 100 characters");

        RuleFor(x => x.RegistrationNumber)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.RegistrationNumber))
            .WithMessage("Registration number cannot exceed 100 characters");

        RuleFor(x => x.TaxId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.TaxId))
            .WithMessage("Tax ID cannot exceed 100 characters");
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
