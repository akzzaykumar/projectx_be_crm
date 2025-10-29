using FluentValidation;

namespace ActivoosCRM.Application.Features.Reviews.Commands.MarkReviewHelpful;

/// <summary>
/// Validator for MarkReviewHelpfulCommand
/// </summary>
public class MarkReviewHelpfulCommandValidator : AbstractValidator<MarkReviewHelpfulCommand>
{
    public MarkReviewHelpfulCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("Review ID is required");
    }
}
