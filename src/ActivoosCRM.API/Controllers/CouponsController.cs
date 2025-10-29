using ActivoosCRM.Application.Features.Coupons.Queries.GetMyCoupons;
using ActivoosCRM.Application.Features.Coupons.Queries.ValidateCoupon;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Controller for managing coupons and discount codes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CouponsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CouponsController> _logger;

    public CouponsController(IMediator mediator, ILogger<CouponsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Validate a coupon code for a specific activity and order amount
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/coupons/validate/FIRSTTIME20?activityId=550e8400-e29b-41d4-a716-446655440001&amp;orderAmount=5000.00
    /// 
    /// **Query Parameters:**
    /// - `activityId` (guid, required): The ID of the activity the coupon will be applied to
    /// - `orderAmount` (decimal, required): The total order amount before discount
    /// 
    /// **Validation Rules:**
    /// 1. Coupon must exist in the system
    /// 2. Coupon must be currently active (IsActive = true)
    /// 3. Current date must be within the coupon's validity period (ValidFrom to ValidUntil)
    /// 4. Coupon must not have exceeded its global usage limit (if set)
    /// 5. User must not have already used this coupon (one use per user)
    /// 6. Order amount must meet the minimum order requirement (if set)
    /// 7. Coupon must be applicable to the activity's category (if category restrictions exist)
    /// 
    /// **Discount Calculation:**
    /// - **Percentage Discount**: Calculates as (orderAmount × discountValue / 100), capped at MaxDiscountAmount if set
    /// - **Fixed Amount Discount**: Applies the fixed discountValue directly
    /// - Discount cannot exceed the order amount
    /// 
    /// **Response:**
    /// - Returns validation result with `isValid` flag and detailed `validationMessage`
    /// - If valid: includes `discountAmount` and `finalAmount` (order amount minus discount)
    /// - If invalid: explains why the coupon cannot be used
    /// 
    /// **Use Cases:**
    /// - Before checkout: Validate coupon and show discount preview to user
    /// - During booking creation: Confirm coupon is still valid before applying discount
    /// - Real-time validation: Check coupon as user types the code
    /// </remarks>
    /// <param name="code">The coupon code to validate (case-insensitive)</param>
    /// <param name="activityId">The activity ID to apply the coupon to</param>
    /// <param name="orderAmount">The order amount before discount</param>
    /// <returns>Coupon validation result with discount calculation</returns>
    /// <response code="200">Returns validation result (isValid true or false with details)</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("validate/{code}")]
    [ProducesResponseType(typeof(CouponValidationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ValidateCoupon(
        [FromRoute] string code,
        [FromQuery] Guid activityId,
        [FromQuery] decimal orderAmount)
    {
        var query = new ValidateCouponQuery(code, activityId, orderAmount);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get user's available coupons
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/coupons/my-coupons
    /// 
    /// **Returns:**
    /// - List of all active coupons available in the system
    /// - Each coupon includes full details: code, description, discount type and value, validity period
    /// - `canUse` flag indicates if the current user can use this coupon
    /// - `cannotUseReason` explains why a coupon cannot be used (if applicable)
    /// 
    /// **Coupon Usability Checks:**
    /// 1. **Is Active**: Coupon must be active (not deactivated by admin)
    /// 2. **Validity Period**: Current date must be between ValidFrom and ValidUntil
    /// 3. **Usage Limits**: Coupon must not have reached its global usage limit
    /// 4. **User Usage**: User must not have already used this coupon
    /// 
    /// **Business Rules:**
    /// - Each user can use a coupon only once (tracked via CouponUsages)
    /// - Coupons with category restrictions apply only to activities in those categories
    /// - Minimum order amount must be met for coupon to be applicable
    /// - Percentage discounts are capped at MaxDiscountAmount (if set)
    /// 
    /// **Use Cases:**
    /// - Display available coupons to user before checkout
    /// - Show "My Coupons" page with all usable and unusable coupons
    /// - Promotional banners showing active coupons with validity dates
    /// - Help users discover coupons they can apply to their bookings
    /// 
    /// **Response Details:**
    /// - Coupons are ordered by creation date (newest first)
    /// - Includes both usable and unusable coupons (filter by canUse flag)
    /// - Shows usage statistics (usageLimit, usedCount) for transparency
    /// </remarks>
    /// <returns>List of user's available coupons with usability status</returns>
    /// <response code="200">Returns list of coupons with usability flags</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("my-coupons")]
    [ProducesResponseType(typeof(List<UserCouponDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetMyCoupons()
    {
        var query = new GetMyCouponsQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
