using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Notifications.Commands.MarkNotificationAsRead;

/// <summary>
/// Handler for MarkNotificationAsReadCommand
/// Marks a notification as read
/// </summary>
public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MarkNotificationAsReadCommandHandler> _logger;

    public MarkNotificationAsReadCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<MarkNotificationAsReadCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (currentUserId == null || currentUserId == Guid.Empty)
                return Result<bool>.CreateFailure("User not authenticated");

            // Get notification
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == currentUserId, cancellationToken);

            if (notification == null)
                return Result<bool>.CreateFailure("Notification not found");

            // Mark as read
            notification.MarkAsRead();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}",
                notification.Id, currentUserId);

            return Result<bool>.CreateSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", request.NotificationId);
            return Result<bool>.CreateFailure("Failed to mark notification as read. Please try again.");
        }
    }
}
