using FluentValidation;

namespace ActivoosCRM.Application.Features.Activities.Commands.UpdateActivity;

/// <summary>
/// Validator for UpdateActivityCommand
/// </summary>
public class UpdateActivityCommandValidator : AbstractValidator<UpdateActivityCommand>
{
    public UpdateActivityCommandValidator()
    {
        RuleFor(x => x.ActivityId)
            .NotEmpty()
            .WithMessage("Activity ID is required");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MinimumLength(50)
            .WithMessage("Description must be at least 50 characters")
            .MaximumLength(5000)
            .WithMessage("Description must not exceed 5000 characters");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price cannot be negative");

        RuleFor(x => x.MinParticipants)
            .GreaterThan(0)
            .WithMessage("Min participants must be greater than 0");

        RuleFor(x => x.MaxParticipants)
            .GreaterThanOrEqualTo(x => x.MinParticipants)
            .WithMessage("Max participants must be greater than or equal to min participants");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .WithMessage("Duration must be greater than 0 minutes");
    }
}
