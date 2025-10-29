using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Features.Bookings.Commands.CancelBooking;
using ActivoosCRM.Application.Features.Bookings.Commands.CheckInBooking;
using ActivoosCRM.Application.Features.Bookings.Commands.CompleteBooking;
using ActivoosCRM.Application.Features.Bookings.Commands.ConfirmBooking;
using ActivoosCRM.Application.Features.Bookings.Commands.CreateBooking;
using ActivoosCRM.Application.Features.Bookings.Queries.GetBookingById;
using ActivoosCRM.Application.Features.Bookings.Queries.GetBookings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Bookings controller - Manages booking operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BookingsController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;

    public BookingsController(
        IMediator mediator,
        ILogger<BookingsController> logger,
        IConfiguration configuration,
        IApplicationDbContext context)
    {
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;
        _context = context;
    }

    /// <summary>
    /// Get user's bookings with filters and pagination
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/Bookings?page=1&amp;pageSize=10&amp;status=Confirmed&amp;fromDate=2025-01-01&amp;toDate=2025-12-31
    ///
    /// Query parameters:
    /// - **page**: Page number (default: 1)
    /// - **pageSize**: Items per page (default: 10, max: 50)
    /// - **status**: Filter by booking status (Pending/Confirmed/Completed/Cancelled/Refunded)
    /// - **fromDate**: Filter bookings from date (format: yyyy-MM-dd)
    /// - **toDate**: Filter bookings to date (format: yyyy-MM-dd)
    /// - **activityId**: Filter by specific activity GUID
    ///
    /// Returns paginated list of user's bookings with:
    /// - Booking details (reference, date, time, participants, status)
    /// - Total amount and currency
    /// - Activity information (title, location, provider)
    /// - Payment status
    /// - Actions (canBeCancelled flag)
    /// </remarks>
    /// <response code="200">Returns paginated list of bookings</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetBookings([FromQuery] GetBookingsQuery query)
    {
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get booking details by ID
    /// </summary>
    /// <param name="id">Booking GUID</param>
    /// <remarks>
    /// Returns complete booking information including:
    /// - Full booking details with pricing breakdown
    /// - Activity details (title, description, duration, location, provider)
    /// - Customer information
    /// - Payment details and status
    /// - Participant list with details
    /// - Computed flags (canBeCancelled, isPaid, isUpcoming)
    /// - All timestamps (created, confirmed, completed, cancelled, checked-in)
    ///
    /// Authorization:
    /// - Customers can view their own bookings
    /// - Providers can view bookings for their activities
    /// </remarks>
    /// <response code="200">Returns booking details</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized to view this booking</response>
    /// <response code="404">Booking not found</response>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetBookingById(Guid id)
    {
        var result = await _mediator.Send(new GetBookingByIdQuery(id));

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(result);
            }
            if (result.Message.Contains("permission"))
            {
                return Forbid();
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Cancel a booking
    /// </summary>
    /// <param name="id">Booking GUID</param>
    /// <param name="command">Cancellation details</param>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/Bookings/550e8400-e29b-41d4-a716-446655440001/cancel
    ///     {
    ///       "cancellationReason": "Change of plans"
    ///     }
    ///
    /// Cancellation policy and refunds:
    /// - **48+ hours before**: Full refund (100%)
    /// - **24-48 hours before**: Partial refund (50%)
    /// - **Less than 24 hours**: No refund
    ///
    /// Process:
    /// 1. Validates customer owns the booking
    /// 2. Checks if booking can be cancelled (status: Pending or Confirmed)
    /// 3. Calculates refund amount based on time until booking
    /// 4. Cancels booking and processes refund if applicable
    /// 5. Returns refund details
    ///
    /// Refunds are processed within 5-7 business days to original payment method.
    /// </remarks>
    /// <response code="200">Booking cancelled successfully</response>
    /// <response code="400">Cannot cancel booking (wrong status or validation errors)</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized to cancel this booking</response>
    /// <response code="404">Booking not found</response>
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CancelBooking(Guid id, [FromBody] CancelBookingRequest request)
    {
        var command = new CancelBookingCommand(id, request.CancellationReason);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(result);
            }
            if (result.Message.Contains("permission"))
            {
                return Forbid();
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Confirm a booking (Provider only)
    /// </summary>
    /// <param name="id">Booking GUID</param>
    /// <remarks>
    /// Provider action to confirm a pending booking.
    ///
    /// Requirements:
    /// - User must be authenticated as Activity Provider
    /// - Provider must own the activity
    /// - Booking must be in Pending status
    ///
    /// Note: Bookings are auto-confirmed when payment is captured through webhook
    /// </remarks>
    /// <response code="200">Booking confirmed successfully</response>
    /// <response code="400">Cannot confirm booking (wrong status)</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized - Provider only</response>
    /// <response code="404">Booking not found</response>
    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "ActivityProvider")]
    public async Task<IActionResult> ConfirmBooking(Guid id)
    {
        var result = await _mediator.Send(new ConfirmBookingCommand(id));

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(result);
            }
            if (result.Message.Contains("permission"))
            {
                return Forbid();
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Mark booking as completed (Provider only)
    /// </summary>
    /// <param name="id">Booking GUID</param>
    /// <remarks>
    /// Provider action to mark a confirmed booking as completed after the activity.
    ///
    /// Requirements:
    /// - User must be authenticated as Activity Provider
    /// - Provider must own the activity
    /// - Booking must be in Confirmed status
    /// - Booking date must have passed
    ///
    /// This action enables the customer to leave a review.
    /// </remarks>
    /// <response code="200">Booking completed successfully</response>
    /// <response code="400">Cannot complete booking (wrong status or date not passed)</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized - Provider only</response>
    /// <response code="404">Booking not found</response>
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "ActivityProvider")]
    public async Task<IActionResult> CompleteBooking(Guid id)
    {
        var result = await _mediator.Send(new CompleteBookingCommand(id));

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(result);
            }
            if (result.Message.Contains("permission"))
            {
                return Forbid();
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Check-in a customer for booking (Provider only)
    /// </summary>
    /// <param name="id">Booking GUID</param>
    /// <remarks>
    /// Provider action to check-in a customer on the day of the activity.
    ///
    /// Requirements:
    /// - User must be authenticated as Activity Provider
    /// - Provider must own the activity
    /// - Booking must be in Confirmed status
    /// - Booking date must be today
    ///
    /// Use this to track customer attendance and manage capacity.
    /// </remarks>
    /// <response code="200">Customer checked-in successfully</response>
    /// <response code="400">Cannot check-in (wrong status or date not today)</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized - Provider only</response>
    /// <response code="404">Booking not found</response>
    [HttpPut("{id}/checkin")]
    [Authorize(Roles = "ActivityProvider")]
    public async Task<IActionResult> CheckInBooking(Guid id)
    {
        var result = await _mediator.Send(new CheckInBookingCommand(id));

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(result);
            }
            if (result.Message.Contains("permission"))
            {
                return Forbid();
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new booking
    /// </summary>
    /// <param name="command">Booking creation details</param>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/Bookings
    ///     {
    ///       "activityId": "550e8400-e29b-41d4-a716-446655440001",
    ///       "bookingDate": "2025-11-15",
    ///       "bookingTime": "09:00:00",
    ///       "numberOfParticipants": 2,
    ///       "specialRequests": "Vegetarian lunch required",
    ///       "participantNames": "John Doe, Jane Doe",
    ///       "customerNotes": "First time diving",
    ///       "participants": [
    ///         {
    ///           "name": "John Doe",
    ///           "age": 28,
    ///           "gender": "Male",
    ///           "contactPhone": "+919876543210"
    ///         },
    ///         {
    ///           "name": "Jane Doe",
    ///           "age": 26,
    ///           "gender": "Female"
    ///         }
    ///       ],
    ///       "couponCode": "FIRSTTIME20"
    ///     }
    ///
    /// Requirements:
    /// - User must be authenticated with Customer profile
    /// - Activity must be active and available
    /// - Number of participants must be within activity limits
    /// - Booking date must not be in the past
    ///
    /// Process:
    /// 1. Validates activity availability
    /// 2. Checks participant count against activity limits
    /// 3. Applies coupon discount if provided (if valid)
    /// 4. Calculates total amount
    /// 5. Creates booking in Pending status
    /// 6. Adds participant details if provided
    /// 7. Returns booking ID and reference for payment
    ///
    /// Next step: Proceed to /api/Payments/initiate with the bookingId to complete payment
    /// </remarks>
    /// <response code="201">Booking created successfully</response>
    /// <response code="400">Validation errors or business rule violations</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(
            nameof(GetBookingById),
            new { id = result.Data!.BookingId },
            result);
    }

    /// <summary>
    /// Payment gateway webhook handler (Razorpay)
    /// </summary>
    /// <param name="request">Webhook payload from payment gateway</param>
    /// <remarks>
    /// This endpoint receives callbacks from the payment gateway (Razorpay) to update payment status.
    ///
    /// Webhook events handled:
    /// - **payment.captured**: Payment successfully completed
    /// - **payment.failed**: Payment attempt failed
    /// - **refund.created**: Refund processed
    ///
    /// Process:
    /// 1. Verifies webhook signature for security
    /// 2. Validates payment gateway transaction ID
    /// 3. Updates payment status in database
    /// 4. Auto-confirms booking if payment successful
    /// 5. Logs event for monitoring
    ///
    /// Security:
    /// - Signature verification using HMAC-SHA256 with gateway secret key
    /// - Idempotency handling for duplicate webhooks
    /// - All events are logged for audit trail
    ///
    /// Configuration:
    /// - Set RazorpayWebhookSecret in appsettings.json
    /// - Configure this URL in Razorpay dashboard: https://api.funbookr.com/api/Bookings/webhook/payment
    /// </remarks>
    /// <response code="200">Webhook processed successfully</response>
    /// <response code="400">Invalid signature or payload</response>
    [HttpPost("webhook/payment")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentWebhook([FromBody] PaymentWebhookRequest request)
    {
        try
        {
            _logger.LogInformation("Payment webhook received: {Event}", request.Event);

            // Verify webhook signature
            var signature = Request.Headers["X-Razorpay-Signature"].ToString();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Webhook signature missing");
                return BadRequest(new { success = false, message = "Signature required" });
            }

            var isValid = VerifyRazorpaySignature(request, signature);
            if (!isValid)
            {
                _logger.LogWarning("Invalid webhook signature for event: {Event}", request.Event);
                return BadRequest(new { success = false, message = "Invalid signature" });
            }

            // Process webhook based on event type
            await ProcessWebhookEvent(request);

            _logger.LogInformation("Payment webhook processed successfully: {Event}", request.Event);
            return Ok(new { success = true, message = "Webhook processed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment webhook");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private bool VerifyRazorpaySignature(PaymentWebhookRequest request, string signature)
    {
        try
        {
            var webhookSecret = _configuration["Razorpay:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogWarning("Razorpay webhook secret not configured");
                return false;
            }

            // Serialize payload to JSON
            var payload = JsonSerializer.Serialize(request.Payload);
            var encoding = new UTF8Encoding();
            var keyBytes = encoding.GetBytes(webhookSecret);
            var payloadBytes = encoding.GetBytes(payload);

            // Compute HMAC-SHA256
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(payloadBytes);
            var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

            // Compare signatures
            return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Razorpay signature");
            return false;
        }
    }

    private async Task ProcessWebhookEvent(PaymentWebhookRequest request)
    {
        if (request.Payload == null)
        {
            _logger.LogWarning("Webhook payload is null");
            return;
        }

        switch (request.Event)
        {
            case "payment.captured":
                await HandlePaymentCaptured(request.Payload);
                break;

            case "payment.failed":
                await HandlePaymentFailed(request.Payload);
                break;

            case "refund.created":
                _logger.LogInformation("Refund webhook received - no action required");
                break;

            default:
                _logger.LogInformation("Unhandled webhook event: {Event}", request.Event);
                break;
        }
    }

    private async Task HandlePaymentCaptured(Dictionary<string, object> payload)
    {
        try
        {
            // Extract payment details from payload
            if (!payload.ContainsKey("payment"))
            {
                _logger.LogWarning("Payment object missing in payload");
                return;
            }

            var paymentObj = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(payload["payment"]));

            var transactionId = paymentObj.GetProperty("id").GetString();
            var amount = paymentObj.GetProperty("amount").GetInt64() / 100m; // Convert paise to rupees
            var method = paymentObj.TryGetProperty("method", out var methodProp)
                ? methodProp.GetString() : null;
            var cardLast4 = paymentObj.TryGetProperty("card", out var cardProp)
                && cardProp.TryGetProperty("last4", out var last4Prop)
                ? last4Prop.GetString() : null;
            var cardBrand = paymentObj.TryGetProperty("card", out var cardBrand2)
                && cardBrand2.TryGetProperty("network", out var networkProp)
                ? networkProp.GetString() : null;

            if (string.IsNullOrEmpty(transactionId))
            {
                _logger.LogWarning("Transaction ID missing in payment webhook");
                return;
            }

            // Find payment by gateway transaction ID
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentGatewayTransactionId == transactionId);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for transaction ID: {TransactionId}", transactionId);
                return;
            }

            // Check idempotency - if already completed, skip
            if (payment.Status == Domain.Enums.PaymentStatus.Completed)
            {
                _logger.LogInformation("Payment already marked as completed: {TransactionId}", transactionId);
                return;
            }

            // Mark payment as completed
            var gatewayResponse = JsonSerializer.Serialize(payload);
            payment.MarkAsCompleted(
                transactionId,
                method,
                cardLast4,
                cardBrand,
                gatewayResponse);

            // Auto-confirm booking
            if (payment.Booking != null &&
                payment.Booking.Status == Domain.Enums.BookingStatus.Pending)
            {
                // Use system user ID for auto-confirmation (or a specific webhook user ID)
                var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                payment.Booking.Confirm(systemUserId);

                _logger.LogInformation(
                    "Booking auto-confirmed: {BookingId} for payment: {TransactionId}",
                    payment.Booking.Id,
                    transactionId);
            }

            await _context.SaveChangesAsync(default);

            // TODO: Send email/push notification to customer
            _logger.LogInformation("Payment captured successfully: {TransactionId}", transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment.captured webhook");
            throw;
        }
    }

    private async Task HandlePaymentFailed(Dictionary<string, object> payload)
    {
        try
        {
            // Extract payment details
            if (!payload.ContainsKey("payment"))
            {
                _logger.LogWarning("Payment object missing in payload");
                return;
            }

            var paymentObj = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(payload["payment"]));

            var transactionId = paymentObj.GetProperty("id").GetString();
            var errorReason = paymentObj.TryGetProperty("error_description", out var reasonProp)
                ? reasonProp.GetString() : "Payment failed";

            if (string.IsNullOrEmpty(transactionId))
            {
                _logger.LogWarning("Transaction ID missing in payment failed webhook");
                return;
            }

            // Find payment
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentGatewayTransactionId == transactionId);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for transaction ID: {TransactionId}", transactionId);
                return;
            }

            // Mark payment as failed
            var gatewayResponse = JsonSerializer.Serialize(payload);
            payment.MarkAsFailed(errorReason, gatewayResponse);

            await _context.SaveChangesAsync(default);

            // TODO: Send email notification to customer about failed payment
            _logger.LogInformation("Payment marked as failed: {TransactionId}", transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment.failed webhook");
            throw;
        }
    }
}

/// <summary>
/// Payment webhook request model
/// </summary>
public record PaymentWebhookRequest
{
    public string Event { get; init; } = string.Empty;
    public Dictionary<string, object>? Payload { get; init; }
}

/// <summary>
/// Cancel booking request model
/// </summary>
public record CancelBookingRequest
{
    public string CancellationReason { get; init; } = string.Empty;
}

