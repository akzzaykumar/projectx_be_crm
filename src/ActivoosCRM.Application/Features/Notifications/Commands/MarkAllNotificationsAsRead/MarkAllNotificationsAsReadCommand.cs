using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

/// <summary>
/// Command to mark all user notifications as read
/// </summary>
public record MarkAllNotificationsAsReadCommand : IRequest<Result<int>>
{
    // No parameters needed - marks all notifications for current user
}
