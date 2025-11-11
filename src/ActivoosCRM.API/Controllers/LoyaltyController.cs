using ActivoosCRM.Application.Features.Loyalty.Commands.RedeemPoints;
using ActivoosCRM.Application.Features.Loyalty.Queries.GetLoyaltyHistory;
using ActivoosCRM.Application.Features.Loyalty.Queries.GetLoyaltyStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Loyalty controller - Manages loyalty points and rewards
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoyaltyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LoyaltyController> _logger;

    public LoyaltyController(
        IMediator mediator,
        ILogger<LoyaltyController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's loyalty status
    /// </summary>
    /// <remarks>
    /// Returns complete loyalty program status for the authenticated user.
    /// 
    /// **Information included:**
    /// - Current tier (Bronze/Silver/Gold/Platinum)
    /// - Total points accumulated
    /// - Available points for redemption
    /// - Lifetime points earned
    /// - Discount percentage for current tier
    /// - Points needed to reach next tier
    /// - Tier benefits and perks
    /// 
    /// **Tier Progression:**
    /// - Bronze: 0 points (0% discount)
    /// - Silver: 5,000 points (5% discount)
    /// - Gold: 20,000 points (10% discount)
    /// - Platinum: 50,000 points (15% discount)
    /// 
    /// **Example:**
    ///  
    ///     GET /api/loyalty/status
    /// </remarks>
    /// <response code="200">Loyalty status retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(LoyaltyStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLoyaltyStatus()
    {
        _logger.LogInformation("Getting loyalty status for current user");

        var query = new GetLoyaltyStatusQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get loyalty transaction history
    /// </summary>
    /// <remarks>
    /// Returns the transaction history showing how points were earned and redeemed.
    /// 
    /// **Transaction Types:**
    /// - `earned` - Points earned (booking, review, referral)
    /// - `redeemed` - Points redeemed for discounts
    /// 
    /// **Example Response:**
    /// 
    ///     GET /api/loyalty/history?pageSize=50
    ///     
    ///     [
    ///       {
    ///         "points": 2500,
    ///         "transactionType": "earned",
    ///         "description": "Booking completed - Skydiving Adventure",
    ///         "createdAt": "2025-11-01T10:30:00Z",
    ///         "expiryDate": "2026-11-01T10:30:00Z",
    ///         "isExpired": false
    ///       },
    ///       {
    ///         "points": -400,
    ///         "transactionType": "redeemed",
    ///         "description": "Redeemed 400 points for ₹100 discount",
    ///         "createdAt": "2025-10-28T14:20:00Z"
    ///       }
    ///     ]
    /// </remarks>
    /// <param name="pageSize">Number of transactions to retrieve (max: 200)</param>
    /// <response code="200">Transaction history retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<LoyaltyHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactionHistory([FromQuery] int pageSize = 50)
    {
        _logger.LogInformation("Getting loyalty history for current user, pageSize: {PageSize}", pageSize);

        var query = new GetLoyaltyHistoryQuery { PageSize = pageSize };
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Redeem loyalty points for booking discount
    /// </summary>
    /// <remarks>
    /// Redeem accumulated points to get a discount on a booking.
    /// 
    /// **Redemption Rules:**
    /// - Minimum redemption: 100 points
    /// - Maximum per transaction: 100,000 points
    /// - Conversion rate: 100 points = ₹25
    /// - Can only redeem on unpaid bookings
    /// - Must own the booking
    /// 
    /// **Example:**
    /// 
    ///     POST /api/loyalty/redeem
    ///     {
    ///         "bookingId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "points": 400
    ///     }
    ///     
    ///     Response:
    ///     {
    ///         "bookingId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "pointsRedeemed": 400,
    ///         "discountAmount": 100.00,
    ///         "remainingPoints": 2100,
    ///         "updatedBookingTotal": 1400.00
    ///     }
    /// </remarks>
    /// <response code="200">Points redeemed successfully</response>
    /// <response code="400">Insufficient points or invalid request</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Booking not found</response>
    [HttpPost("redeem")]
    [ProducesResponseType(typeof(RedeemPointsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedeemPoints([FromBody] RedeemPointsCommand command)
    {
        _logger.LogInformation(
            "Redeeming {Points} points for booking {BookingId}",
            command.Points, command.BookingId);

        var result = await _mediator.Send(command);

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

    /// <summary>
    /// Get loyalty program information
    /// </summary>
    /// <remarks>
    /// Returns general information about the loyalty program.
    /// This endpoint is public to allow users to learn about the program before signing up.
    /// 
    /// **Program Details:**
    /// - Earn 1 point per ₹1 spent
    /// - 50 points for each review
    /// - 100 points for detailed reviews
    /// - 250 bonus points for first booking
    /// - 500 points for successful referrals
    /// 
    /// **Redemption:**
    /// - 100 points = ₹25 discount
    /// - Points valid for 365 days
    /// 
    /// **Example:**
    /// 
    ///     GET /api/loyalty/program-info
    /// </remarks>
    /// <response code="200">Program information retrieved</response>
    [HttpGet("program-info")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoyaltyProgramInfoResponse), StatusCodes.Status200OK)]
    public IActionResult GetProgramInfo()
    {
        var response = new LoyaltyProgramInfoResponse
        {
            ProgramName = "FunBookr Rewards",
            Description = "Earn points on every booking and redeem them for amazing discounts!",
            PointsEarningRules = new List<PointsEarningRuleDto>
            {
                new() { Action = "Spend ₹1", Points = 1, Description = "Earn 1 point for every rupee spent" },
                new() { Action = "Write a review", Points = 50, Description = "Share your experience" },
                new() { Action = "Detailed review (100+ characters)", Points = 100, Description = "Detailed feedback earns bonus points" },
                new() { Action = "First booking", Points = 250, Description = "One-time welcome bonus" },
                new() { Action = "Refer a friend", Points = 500, Description = "When they complete their first booking" }
            },
            RedemptionRate = "100 points = ₹25 discount",
            MinimumRedemption = 100,
            PointsExpiry = "365 days from earning",
            Tiers = new List<LoyaltyTierInfoDto>
            {
                new() { Name = "Bronze", PointsRequired = 0, DiscountPercentage = 0, Benefits = new[] { "Earn points on bookings", "Redeem for discounts", "Exclusive member deals" } },
                new() { Name = "Silver", PointsRequired = 5000, DiscountPercentage = 5, Benefits = new[] { "All Bronze benefits", "5% discount on all bookings", "Priority support", "Early access to sales" } },
                new() { Name = "Gold", PointsRequired = 20000, DiscountPercentage = 10, Benefits = new[] { "All Silver benefits", "10% discount on all bookings", "Free rescheduling", "Birthday specials", "Account manager" } },
                new() { Name = "Platinum", PointsRequired = 50000, DiscountPercentage = 15, Benefits = new[] { "All Gold benefits", "15% discount on all bookings", "VIP experiences", "Concierge service", "Complimentary upgrades", "Partner discounts" } }
            }
        };

        return Ok(new { success = true, data = response });
    }
}

/// <summary>
/// Loyalty program information response
/// </summary>
public class LoyaltyProgramInfoResponse
{
    public string ProgramName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PointsEarningRuleDto> PointsEarningRules { get; set; } = new();
    public string RedemptionRate { get; set; } = string.Empty;
    public int MinimumRedemption { get; set; }
    public string PointsExpiry { get; set; } = string.Empty;
    public List<LoyaltyTierInfoDto> Tiers { get; set; } = new();
}

/// <summary>
/// Points earning rule DTO
/// </summary>
public class PointsEarningRuleDto
{
    public string Action { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Loyalty tier information DTO
/// </summary>
public class LoyaltyTierInfoDto
{
    public string Name { get; set; } = string.Empty;
    public int PointsRequired { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string[] Benefits { get; set; } = Array.Empty<string>();
}