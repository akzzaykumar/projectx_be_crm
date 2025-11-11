using ActivoosCRM.Application.Features.GiftCards.Commands.ApplyGiftCard;
using ActivoosCRM.Application.Features.GiftCards.Commands.CreateGiftCard;
using ActivoosCRM.Application.Features.GiftCards.Queries.GetGiftCardBalance;
using ActivoosCRM.Application.Features.GiftCards.Queries.GetUserGiftCards;
using ActivoosCRM.Application.Features.GiftCards.Queries.ValidateGiftCard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Gift Cards controller - Manages gift card operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GiftCardsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GiftCardsController> _logger;

    public GiftCardsController(
        IMediator mediator,
        ILogger<GiftCardsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new gift card
    /// </summary>
    /// <remarks>
    /// Creates a new gift card with the specified amount. 
    /// If recipient email is provided, the gift card details will be sent to them.
    /// 
    /// **Business Rules:**
    /// - Minimum amount: ₹500
    /// - Maximum amount: ₹50,000
    /// - Validity: 365 days from purchase
    /// - Supported currencies: INR, USD
    /// 
    /// **Examples:**
    /// 
    ///     POST /api/giftcards
    ///     {
    ///         "amount": 2500,
    ///         "currency": "INR",
    ///         "recipientEmail": "friend@example.com",
    ///         "recipientName": "John Doe",
    ///         "message": "Happy Birthday! Enjoy an amazing experience!"
    ///     }
    /// </remarks>
    /// <response code="201">Gift card created successfully</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CreateGiftCardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateGiftCard([FromBody] CreateGiftCardCommand command)
    {
        _logger.LogInformation("Creating gift card with amount: {Amount}", command.Amount);

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            _logger.LogWarning("Gift card creation failed: {Message}", result.Message);
            return BadRequest(result);
        }

        return CreatedAtAction(
            nameof(GetGiftCardBalance),
            new { code = result.Data!.Code },
            result);
    }

    /// <summary>
    /// Validate a gift card code
    /// </summary>
    /// <remarks>
    /// Validates a gift card code and returns its validity status and balance.
    /// This endpoint is public (no authentication required) to allow checking before applying.
    /// 
    /// **Validation Checks:**
    /// - Code format is correct
    /// - Gift card exists
    /// - Not expired
    /// - Has remaining balance
    /// - Is active (not cancelled)
    /// 
    /// **Example:**
    /// 
    ///     GET /api/giftcards/validate/FB-1234-5678-9012
    /// </remarks>
    /// <param name="code">Gift card code (format: FB-XXXX-XXXX-XXXX)</param>
    /// <response code="200">Validation result with balance information</response>
    /// <response code="400">Invalid code format</response>
    [HttpGet("validate/{code}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ValidateGiftCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateGiftCard(string code)
    {
        _logger.LogInformation("Validating gift card: {Code}", code);

        var query = new ValidateGiftCardQuery { Code = code };
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Apply gift card to a booking
    /// </summary>
    /// <remarks>
    /// Applies a gift card to reduce the booking amount.
    /// The gift card amount will be deducted from the booking total.
    /// 
    /// **Business Rules:**
    /// - User must own the booking
    /// - Booking must not be already paid
    /// - Gift card must be valid and have sufficient balance
    /// - Partial usage supported (remaining balance preserved)
    /// 
    /// **Example:**
    /// 
    ///     POST /api/giftcards/apply
    ///     {
    ///         "bookingId": "guid",
    ///         "code": "FB-1234-5678-9012"
    ///     }
    /// </remarks>
    /// <response code="200">Gift card applied successfully</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Booking or gift card not found</response>
    [HttpPost("apply")]
    [Authorize]
    [ProducesResponseType(typeof(ApplyGiftCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyGiftCard([FromBody] ApplyGiftCardCommand command)
    {
        _logger.LogInformation(
            "Applying gift card {Code} to booking {BookingId}",
            command.Code, command.BookingId);

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
    /// Get current user's gift cards
    /// </summary>
    /// <remarks>
    /// Returns all gift cards purchased by or sent to the current user.
    /// Includes both active and redeemed cards with detailed status.
    /// 
    /// **Response includes:**
    /// - All purchased gift cards
    /// - All received gift cards (sent to  user's email)
    /// - Balance and expiry information
    /// - Usage status
    /// 
    /// **Example:**
    /// 
    ///     GET /api/giftcards/my-cards
    /// </remarks>
    /// <response code="200">List of user's gift cards</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("my-cards")]
    [Authorize]
    [ProducesResponseType(typeof(List<UserGiftCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyGiftCards()
    {
        _logger.LogInformation("Getting gift cards for current user");

        var query = new GetUserGiftCardsQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get gift card balance by code
    /// </summary>
    /// <remarks>
    /// Check the current balance and status of a gift card.
    /// This endpoint is public to allow balance checking before usage.
    /// 
    /// **Information Returned:**
    /// - Current balance
    /// - Original amount
    /// - Expiry date
    /// - Days until expiry
    /// - Usage status
    /// 
    /// **Example:**
    /// 
    ///     GET /api/giftcards/FB-1234-5678-9012/balance
    /// </remarks>
    /// <param name="code">Gift card code (format: FB-XXXX-XXXX-XXXX)</param>
    /// <response code="200">Gift card balance information</response>
    /// <response code="404">Gift card not found</response>
    [HttpGet("{code}/balance")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GiftCardBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGiftCardBalance(string code)
    {
        _logger.LogInformation("Getting balance for gift card: {Code}", code);

        var query = new GetGiftCardBalanceQuery { Code = code };
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