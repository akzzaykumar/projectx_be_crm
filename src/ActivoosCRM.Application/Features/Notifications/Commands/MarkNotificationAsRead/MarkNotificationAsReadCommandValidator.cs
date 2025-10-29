using FluentValidation;

namespace ActivoosCRM.Application.Features.Notifications.Commands.MarkNotificationAsRead;

/// <summary>
/// Validator for MarkNotificationAsReadCommand
/// </summary>
public class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
{
    public MarkNotificationAsReadCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty()
            .WithMessage("Notification ID is required");
    }
}
