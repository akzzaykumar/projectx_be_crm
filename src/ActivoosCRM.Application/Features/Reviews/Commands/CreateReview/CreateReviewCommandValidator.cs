using FluentValidation;

namespace ActivoosCRM.Application.Features.Reviews.Commands.CreateReview;

/// <summary>
/// Validator for CreateReviewCommand
/// </summary>
public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("Booking ID is required");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5");

        RuleFor(x => x.Title)
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.ReviewText)
            .MaximumLength(2000)
            .WithMessage("Review text cannot exceed 2000 characters");
    }
}
