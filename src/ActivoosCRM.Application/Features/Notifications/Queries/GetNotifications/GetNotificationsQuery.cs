using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Notifications.Queries.GetNotifications;

/// <summary>
/// Query to get user's notifications with filters and pagination
/// </summary>
public record GetNotificationsQuery : IRequest<Result<GetNotificationsResponse>>
{
    public bool? IsRead { get; init; }
    public string? Type { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

/// <summary>
/// Response containing paginated notifications with unread count
/// </summary>
public record GetNotificationsResponse
{
    public List<NotificationDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int UnreadCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>
/// Notification data transfer object
/// </summary>
public record NotificationDto
{
    public Guid NotificationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public Guid? RelatedBookingId { get; init; }
    public DateTime CreatedAt { get; init; }
}
