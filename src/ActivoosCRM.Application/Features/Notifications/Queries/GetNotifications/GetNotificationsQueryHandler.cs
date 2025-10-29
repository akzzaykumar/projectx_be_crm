using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Notifications.Queries.GetNotifications;

/// <summary>
/// Handler for GetNotificationsQuery
/// Retrieves user's notifications with filters and pagination
/// </summary>
public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, Result<GetNotificationsResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetNotificationsQueryHandler> _logger;

    public GetNotificationsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetNotificationsQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<GetNotificationsResponse>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (currentUserId == null || currentUserId == Guid.Empty)
                return Result<GetNotificationsResponse>.CreateFailure("User not authenticated");

            // Build query with filters
            var query = _context.Notifications
                .Where(n => n.UserId == currentUserId)
                .AsQueryable();

            // Apply filters
            if (request.IsRead.HasValue)
                query = query.Where(n => n.IsRead == request.IsRead.Value);

            if (!string.IsNullOrWhiteSpace(request.Type))
                query = query.Where(n => n.Type == request.Type.ToLowerInvariant());

            // Calculate total count and unread count
            var totalCount = await query.CountAsync(cancellationToken);
            var unreadCount = await _context.Notifications
                .Where(n => n.UserId == currentUserId && !n.IsRead)
                .CountAsync(cancellationToken);

            // Apply pagination
            var pageSize = Math.Min(request.PageSize, 50); // Max 50 items per page
            var skip = (request.Page - 1) * pageSize;

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    RelatedBookingId = n.RelatedBookingId,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var response = new GetNotificationsResponse
            {
                Items = notifications,
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                PageNumber = request.Page,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            _logger.LogInformation("Retrieved {Count} notifications for user {UserId} (page {Page} of {TotalPages})",
                notifications.Count, currentUserId, request.Page, totalPages);

            return Result<GetNotificationsResponse>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return Result<GetNotificationsResponse>.CreateFailure("Failed to retrieve notifications");
        }
    }
}
