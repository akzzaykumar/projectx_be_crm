using ActivoosCRM.Application.Features.Dashboard.Queries.GetDashboardStatistics;
using ActivoosCRM.Application.Features.Dashboard.Queries.GetRevenueAnalytics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Dashboard controller - Provides analytics and statistics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IMediator mediator,
        ILogger<DashboardController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics for current user
    /// </summary>
    /// <remarks>
    /// Returns role-based dashboard statistics.
    /// 
    /// **For Customers**:
    /// - Total, upcoming, and completed bookings
    /// - Total amount spent
    /// - Favorite activities
    /// - Recent bookings
    /// - Wishlist count
    /// - Unread notifications
    /// 
    /// **For Providers**:
    /// - Total and active activities
    /// - Booking statistics (total, monthly, pending, upcoming)
    /// - Revenue metrics (total and monthly)
    /// - Average rating and review count
    /// - Top performing activities
    /// - Recent bookings
    /// 
    /// **Example Response (Customer)**:
    /// 
    ///     GET /api/dashboard/stats
    ///     {
    ///       "success": true,
    ///       "data": {
    ///         "totalBookings": 15,
    ///         "upcomingBookings": 3,
    ///         "completedBookings": 10,
    ///         "totalSpent": 45600.00,
    ///         "currency": "INR",
    ///         "favoriteActivities": [...],
    ///         "recentBookings": [...],
    ///         "wishlistCount": 5,
    ///         "unreadNotifications": 3
    ///       }
    ///     }
    /// </remarks>
    /// <response code="200">Dashboard statistics retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboardStatistics()
    {
        _logger.LogInformation("Getting dashboard statistics for current user");

        var query = new GetDashboardStatisticsQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get revenue analytics for provider
    /// </summary>
    /// <remarks>
    /// Returns detailed revenue analytics with periodic breakdown.
    /// **Provider only** - Returns 403 if called by non-provider.
    /// 
    /// **Parameters**:
    /// - `period`: Grouping period (daily, weekly, monthly, yearly)
    /// - `fromDate`: Start date (default: 12 months ago)
    /// - `toDate`: End date (default: today)
    /// 
    /// **Response includes**:
    /// - Total revenue for period
    /// - Chart data grouped by period
    /// - Top activities by revenue
    /// - Average booking value
    /// - Total booking count
    /// 
    /// **Example**:
    /// 
    ///     GET /api/dashboard/revenue?period=monthly&fromDate=2025-01-01&toDate=2025-12-31
    ///     
    ///     {
    ///       "success": true,
    ///       "data": {
    ///         "totalRevenue": 1250000.00,
    ///         "currency": "INR",
    ///         "period": "monthly",
    ///         "chartData": [
    ///           { "date": "2025-01", "revenue": 98000, "bookings": 35 },
    ///           { "date": "2025-02", "revenue": 125000, "bookings": 45 }
    ///         ],
    ///         "topActivities": [...],
    ///         "averageBookingValue": 2800.00,
    ///         "totalBookings": 450
    ///       }
    ///     }
    /// </remarks>
    /// <param name="period">Grouping period (daily, weekly, monthly, yearly)</param>
    /// <param name="fromDate">Start date for analytics</param>
    /// <param name="toDate">End date for analytics</param>
    /// <response code="200">Revenue analytics retrieved successfully</response>
    /// <response code="400">Invalid parameters or missing provider profile</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not a provider</response>
    [HttpGet("revenue")]
    [Authorize(Roles = "ActivityProvider")]
    [ProducesResponseType(typeof(RevenueAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRevenueAnalytics(
        [FromQuery] string period = "monthly",
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        _logger.LogInformation("Getting revenue analytics, period: {Period}", period);

        var query = new GetRevenueAnalyticsQuery
        {
            Period = period,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}