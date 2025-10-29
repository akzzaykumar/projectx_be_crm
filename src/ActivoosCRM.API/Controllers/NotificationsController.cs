using ActivoosCRM.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using ActivoosCRM.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using ActivoosCRM.Application.Features.Notifications.Queries.GetNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Notification management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(IMediator mediator, ILogger<NotificationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get user notifications with filters and pagination
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/Notifications?isRead=false&amp;type=booking&amp;page=1&amp;pageSize=10
    /// 
    /// Query Parameters:
    /// - isRead (bool, optional): Filter by read status (true for read, false for unread)
    /// - type (string, optional): Filter by notification type (booking, payment, review, promotion)
    /// - page (int, default=1): Page number
    /// - pageSize (int, default=10, max=50): Items per page
    /// 
    /// Response includes:
    /// - Paginated list of notifications
    /// - Total count of all notifications matching filters
    /// - Unread count (total unread notifications regardless of filters)
    /// - Notification details: title, message, type, read status, related booking ID, created date
    /// 
    /// Notifications are ordered by creation date (newest first).
    /// </remarks>
    /// <param name="isRead">Filter by read status</param>
    /// <param name="type">Filter by notification type</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 50)</param>
    /// <response code="200">Notifications retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetNotificationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? isRead = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetNotificationsQuery
        {
            IsRead = isRead,
            Type = type,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/Notifications/550e8400-e29b-41d4-a716-446655440000/read
    /// 
    /// Marks a single notification as read for the authenticated user.
    /// 
    /// Business Rules:
    /// - User must be authenticated
    /// - Notification must belong to the current user
    /// - Notification must exist
    /// 
    /// Use Case:
    /// - User clicks on a notification in the UI
    /// - User views notification details
    /// - Mark notification as acknowledged
    /// </remarks>
    /// <param name="id">Notification ID</param>
    /// <response code="200">Notification marked as read</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Notification not found</response>
    [HttpPut("{id}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkNotificationAsRead(Guid id)
    {
        var command = new MarkNotificationAsReadCommand { NotificationId = id };
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/Notifications/mark-all-read
    /// 
    /// Marks all unread notifications as read for the authenticated user.
    /// 
    /// Business Rules:
    /// - User must be authenticated
    /// - Only unread notifications are affected
    /// - Returns count of notifications marked as read
    /// 
    /// Use Cases:
    /// - User clicks "Mark all as read" button
    /// - Clear all notifications badge
    /// - Bulk notification management
    /// 
    /// Performance Note:
    /// - Efficiently marks all notifications in a single operation
    /// - Returns immediately if no unread notifications exist
    /// </remarks>
    /// <response code="200">All notifications marked as read (returns count)</response>
    /// <response code="401">User not authenticated</response>
    [HttpPut("mark-all-read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllNotificationsAsRead()
    {
        var command = new MarkAllNotificationsAsReadCommand();
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
