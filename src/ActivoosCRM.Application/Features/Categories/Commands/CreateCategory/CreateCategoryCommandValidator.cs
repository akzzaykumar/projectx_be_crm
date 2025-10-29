using FluentValidation;

namespace ActivoosCRM.Application.Features.Categories.Commands.CreateCategory;

/// <summary>
/// Validator for CreateCategoryCommand
/// </summary>
public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Category slug is required")
            .MaximumLength(100).WithMessage("Category slug must not exceed 100 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Category slug must contain only lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.IconUrl)
            .MaximumLength(500).WithMessage("Icon URL must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.IconUrl));

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be a non-negative number");
    }
}
