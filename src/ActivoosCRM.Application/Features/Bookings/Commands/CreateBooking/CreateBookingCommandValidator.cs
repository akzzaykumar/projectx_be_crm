using FluentValidation;

namespace ActivoosCRM.Application.Features.Bookings.Commands.CreateBooking;

/// <summary>
/// Validator for CreateBookingCommand
/// </summary>
public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.ActivityId)
            .NotEmpty()
            .WithMessage("Activity ID is required");

        RuleFor(x => x.BookingDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Booking date cannot be in the past");

        RuleFor(x => x.NumberOfParticipants)
            .GreaterThan(0)
            .WithMessage("Number of participants must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Number of participants cannot exceed 100");

        RuleFor(x => x.SpecialRequests)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.SpecialRequests))
            .WithMessage("Special requests must not exceed 1000 characters");

        RuleFor(x => x.ParticipantNames)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.ParticipantNames))
            .WithMessage("Participant names must not exceed 500 characters");

        RuleFor(x => x.CustomerNotes)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerNotes))
            .WithMessage("Customer notes must not exceed 1000 characters");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.CouponCode))
            .WithMessage("Coupon code must not exceed 50 characters");
    }
}
