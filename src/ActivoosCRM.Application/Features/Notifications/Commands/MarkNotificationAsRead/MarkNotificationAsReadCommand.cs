using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Notifications.Commands.MarkNotificationAsRead;

/// <summary>
/// Command to mark a notification as read
/// </summary>
public record MarkNotificationAsReadCommand : IRequest<Result<bool>>
{
    public Guid NotificationId { get; init; }
}
