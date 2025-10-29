using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

/// <summary>
/// Handler for MarkAllNotificationsAsReadCommand
/// Marks all unread notifications as read for the current user
/// </summary>
public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MarkAllNotificationsAsReadCommandHandler> _logger;

    public MarkAllNotificationsAsReadCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<MarkAllNotificationsAsReadCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (currentUserId == null || currentUserId == Guid.Empty)
                return Result<int>.CreateFailure("User not authenticated");

            // Get all unread notifications for current user
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == currentUserId && !n.IsRead)
                .ToListAsync(cancellationToken);

            if (!unreadNotifications.Any())
            {
                _logger.LogInformation("No unread notifications found for user {UserId}", currentUserId);
                return Result<int>.CreateSuccess(0);
            }

            // Mark all as read
            foreach (var notification in unreadNotifications)
            {
                notification.MarkAsRead();
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}",
                unreadNotifications.Count, currentUserId);

            return Result<int>.CreateSuccess(unreadNotifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return Result<int>.CreateFailure("Failed to mark notifications as read. Please try again.");
        }
    }
}
