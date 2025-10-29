using FluentValidation;

namespace ActivoosCRM.Application.Features.Activities.Commands.CreateActivity;

/// <summary>
/// Validator for CreateActivityCommand
/// </summary>
public class CreateActivityCommandValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Category is required");

        RuleFor(x => x.LocationId)
            .NotEmpty()
            .WithMessage("Location is required");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Slug is required")
            .MaximumLength(250)
            .WithMessage("Slug must not exceed 250 characters")
            .Matches(@"^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MinimumLength(50)
            .WithMessage("Description must be at least 50 characters")
            .MaximumLength(5000)
            .WithMessage("Description must not exceed 5000 characters");

        RuleFor(x => x.ShortDescription)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.ShortDescription))
            .WithMessage("Short description must not exceed 500 characters");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price cannot be negative");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be a 3-letter code (e.g., INR, USD)");

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(0)
            .WithMessage("Max participants must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Max participants must not exceed 1000");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .WithMessage("Duration must be greater than 0 minutes")
            .LessThanOrEqualTo(10080) // 7 days in minutes
            .WithMessage("Duration must not exceed 7 days");

        RuleFor(x => x.MinAge)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinAge.HasValue)
            .WithMessage("Minimum age cannot be negative")
            .LessThanOrEqualTo(150)
            .When(x => x.MinAge.HasValue)
            .WithMessage("Minimum age must be reasonable");

        RuleFor(x => x.MaxAge)
            .GreaterThanOrEqualTo(x => x.MinAge ?? 0)
            .When(x => x.MaxAge.HasValue && x.MinAge.HasValue)
            .WithMessage("Maximum age must be greater than or equal to minimum age")
            .LessThanOrEqualTo(150)
            .When(x => x.MaxAge.HasValue)
            .WithMessage("Maximum age must be reasonable");

        RuleFor(x => x.DifficultyLevel)
            .Must(level => level == null ||
                  new[] { "beginner", "intermediate", "advanced" }.Contains(level.ToLower()))
            .When(x => !string.IsNullOrWhiteSpace(x.DifficultyLevel))
            .WithMessage("Difficulty level must be beginner, intermediate, or advanced");

        RuleFor(x => x.WhatToBring)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.WhatToBring))
            .WithMessage("What to bring must not exceed 1000 characters");

        RuleFor(x => x.MeetingPoint)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingPoint))
            .WithMessage("Meeting point must not exceed 500 characters");

        RuleFor(x => x.CancellationPolicy)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.CancellationPolicy))
            .WithMessage("Cancellation policy must not exceed 1000 characters");
    }
}
