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
    private readonly IRazorpayService _razorpayService;
    private readonly INotificationService _notificationService;

    public BookingsController(
        IMediator mediator,
        ILogger<BookingsController> logger,
        IConfiguration configuration,
        IApplicationDbContext context,
        IRazorpayService razorpayService,
        INotificationService notificationService)
    {
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;
        _context = context;
        _razorpayService = razorpayService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get user's bookings with filters and pagination
    /// </summary>
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
    /// FIXED: Now searches by order ID first, then updates transaction ID
    /// </summary>
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

            var webhookBody = await new StreamReader(Request.Body).ReadToEndAsync();
            var isValid = _razorpayService.VerifyWebhookSignature(
                webhookBody, 
                signature, 
                _configuration["Razorpay:WebhookSecret"] ?? string.Empty);

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
                await HandleRefundCreated(request.Payload);
                break;

            default:
                _logger.LogInformation("Unhandled webhook event: {Event}", request.Event);
                break;
        }
    }

    private async Task HandlePaymentCaptured(Dictionary<string, object> payload)
    {
        // Use a database transaction for atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();
        
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
            var orderId = paymentObj.TryGetProperty("order_id", out var orderIdProp)
                ? orderIdProp.GetString() : null;
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

            // FIXED: Search by order ID first, then by transaction ID
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Customer)
                        .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => 
                    p.PaymentGatewayOrderId == orderId || 
                    p.PaymentGatewayTransactionId == transactionId);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for order ID: {OrderId} or transaction ID: {TransactionId}", 
                    orderId, transactionId);
                return;
            }

            // Check idempotency - if already completed, skip
            if (payment.Status == Domain.Enums.PaymentStatus.Completed)
            {
                _logger.LogInformation("Payment already marked as completed: {TransactionId}", transactionId);
                await transaction.CommitAsync();
                return;
            }

            // Mark payment as completed
            var gatewayResponse = JsonSerializer.Serialize(payload);
            payment.MarkAsCompleted(
                transactionId!,
                method,
                cardLast4,
                cardBrand,
                gatewayResponse);

            // Auto-confirm booking using system user (created in migrations)
            if (payment.Booking != null &&
                payment.Booking.Status == Domain.Enums.BookingStatus.Pending)
            {
                // Get system user ID from configuration or use a predefined constant
                var systemUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == "system@funbookr.com");
                
                if (systemUser != null)
                {
                    payment.Booking.Confirm(systemUser.Id);
                    
                    _logger.LogInformation(
                        "Booking auto-confirmed: {BookingReference} for payment: {TransactionId}",
                        payment.Booking.BookingReference,
                        transactionId);
                }
                else
                {
                    _logger.LogWarning("System user not found - booking not auto-confirmed");
                }
            }

            await _context.SaveChangesAsync(default);
            await transaction.CommitAsync();

            // Send payment success notification to customer
            if (payment.Booking?.Customer != null)
            {
                try
                {
                    await _notificationService.SendPaymentSuccessAsync(
                        payment.BookingId,
                        payment.Booking.Customer.UserId,
                        amount,
                        default);
                    
                    _logger.LogInformation(
                        "Payment success notification sent for booking: {BookingReference}",
                        payment.Booking.BookingReference);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx,
                        "Failed to send payment success notification for booking: {BookingId}",
                        payment.BookingId);
                }
            }
            
            _logger.LogInformation("Payment captured successfully: {TransactionId}, Amount: {Amount}",
                transactionId, amount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
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
            var orderId = paymentObj.TryGetProperty("order_id", out var orderIdProp)
                ? orderIdProp.GetString() : null;
            var errorReason = paymentObj.TryGetProperty("error_description", out var reasonProp)
                ? reasonProp.GetString() : "Payment failed";

            if (string.IsNullOrEmpty(transactionId))
            {
                _logger.LogWarning("Transaction ID missing in payment failed webhook");
                return;
            }

            // FIXED: Search by order ID first
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => 
                    p.PaymentGatewayOrderId == orderId || 
                    p.PaymentGatewayTransactionId == transactionId);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for order ID: {OrderId} or transaction ID: {TransactionId}", 
                    orderId, transactionId);
                return;
            }

            // Mark payment as failed
            var gatewayResponse = JsonSerializer.Serialize(payload);
            payment.MarkAsFailed(errorReason ?? "Payment failed", gatewayResponse);

            await _context.SaveChangesAsync(default);

            // Send payment failure notification to customer
            if (payment.Booking != null)
            {
                var booking = await _context.Bookings
                    .Include(b => b.Customer)
                    .FirstOrDefaultAsync(b => b.Id == payment.BookingId);
                    
                if (booking?.Customer != null)
                {
                    try
                    {
                        await _notificationService.SendPaymentFailureAsync(
                            payment.BookingId,
                            booking.Customer.UserId,
                            errorReason ?? "Payment failed",
                            default);
                        
                        _logger.LogInformation(
                            "Payment failure notification sent for booking: {BookingReference}",
                            booking.BookingReference);
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogError(notifEx,
                            "Failed to send payment failure notification for booking: {BookingId}",
                            payment.BookingId);
                    }
                }
            }
            
            _logger.LogInformation("Payment marked as failed: {TransactionId}, Reason: {Reason}",
                transactionId, errorReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment.failed webhook");
            throw;
        }
    }

    private async Task HandleRefundCreated(Dictionary<string, object> payload)
    {
        try
        {
            if (!payload.ContainsKey("refund"))
            {
                _logger.LogWarning("Refund object missing in payload");
                return;
            }

            var refundObj = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(payload["refund"]));

            var refundId = refundObj.GetProperty("id").GetString();
            var paymentId = refundObj.GetProperty("payment_id").GetString();
            var amount = refundObj.GetProperty("amount").GetInt64() / 100m;

            _logger.LogInformation(
                "Refund created: {RefundId} for Payment: {PaymentId}, Amount: {Amount}",
                refundId, paymentId, amount);

            // Payment refund status is already updated by the cancellation flow
            // This webhook is just for confirmation and logging
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling refund.created webhook");
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
