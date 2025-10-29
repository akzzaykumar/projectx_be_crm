using FluentValidation;

namespace ActivoosCRM.Application.Features.LocationRequests.Commands.RejectLocationRequest;

public class RejectLocationRequestCommandValidator : AbstractValidator<RejectLocationRequestCommand>
{
    public RejectLocationRequestCommandValidator()
    {
        RuleFor(x => x.LocationRequestId)
            .NotEmpty()
            .WithMessage("Location request ID is required");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .WithMessage("Rejection reason is required")
            .MaximumLength(1000)
            .WithMessage("Rejection reason cannot exceed 1000 characters");
    }
}
