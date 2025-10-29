using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Application.Features.Payments.Commands.InitiatePayment;
using ActivoosCRM.Application.Features.Payments.Queries.GetPaymentById;
using ActivoosCRM.Application.Features.Payments.Queries.GetPaymentMethods;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// API controller for payment operations
/// Handles payment initiation, status tracking, and payment method queries
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get available payment methods with processing fees
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/Payments/methods
    /// 
    /// Response:
    /// 
    ///     {
    ///         "success": true,
    ///         "data": [
    ///             {
    ///                 "method": "UPI",
    ///                 "displayName": "UPI",
    ///                 "isActive": true,
    ///                 "processingFee": 0.00
    ///             },
    ///             {
    ///                 "method": "Card",
    ///                 "displayName": "Credit/Debit Card",
    ///                 "isActive": true,
    ///                 "processingFee": 2.5
    ///             }
    ///         ]
    ///     }
    /// 
    /// </remarks>
    /// <returns>List of available payment methods</returns>
    /// <response code="200">Payment methods retrieved successfully</response>
    [HttpGet("methods")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<List<PaymentMethodDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentMethods()
    {
        var query = new GetPaymentMethodsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Initiate payment for a booking
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/Payments/initiate
    ///     {
    ///         "bookingId": "550e8400-e29b-41d4-a716-446655440000",
    ///         "paymentGateway": "Razorpay"
    ///     }
    /// 
    /// Response:
    /// 
    ///     {
    ///         "success": true,
    ///         "message": "Payment initiated successfully",
    ///         "data": {
    ///             "paymentId": "550e8400-e29b-41d4-a716-446655440010",
    ///             "paymentReference": "PAY20251026XYZ789",
    ///             "amount": 5600.00,
    ///             "currency": "INR",
    ///             "gatewayOrderId": "order_razorpay_123456",
    ///             "gatewayKey": "rzp_live_xxxxxxxxxx",
    ///             "callbackUrl": "https://api.funbookr.com/v1/payments/callback"
    ///         }
    ///     }
    /// 
    /// This endpoint creates a payment record and generates a payment gateway order.
    /// The response contains all necessary information to integrate with the payment gateway on the frontend.
    /// 
    /// For Razorpay integration:
    /// 1. Use the gatewayOrderId and gatewayKey to initialize Razorpay checkout
    /// 2. After successful payment, Razorpay will call the webhook (see BookingsController /webhook/payment)
    /// 3. The webhook will automatically confirm the booking and update payment status
    /// 
    /// </remarks>
    /// <param name="command">Payment initiation details</param>
    /// <returns>Payment gateway details for frontend integration</returns>
    /// <response code="200">Payment initiated successfully</response>
    /// <response code="400">Invalid booking or payment already completed</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User not authorized to pay for this booking</response>
    [HttpPost("initiate")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(Result<InitiatePaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentCommand command)
    {
        _logger.LogInformation("Initiating payment for booking {BookingId} via {Gateway}",
            command.BookingId, command.PaymentGateway);

        var result = await _mediator.Send(command);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Get payment details by ID
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/Payments/550e8400-e29b-41d4-a716-446655440010
    /// 
    /// Response:
    /// 
    ///     {
    ///         "success": true,
    ///         "data": {
    ///             "paymentId": "550e8400-e29b-41d4-a716-446655440010",
    ///             "paymentReference": "PAY20251026XYZ789",
    ///             "bookingId": "550e8400-e29b-41d4-a716-446655440000",
    ///             "amount": 5600.00,
    ///             "currency": "INR",
    ///             "status": "Completed",
    ///             "paymentGateway": "Razorpay",
    ///             "paymentMethod": "UPI",
    ///             "gatewayTransactionId": "txn_razorpay_789123",
    ///             "paidAt": "2025-10-26T14:30:00Z",
    ///             "refundedAmount": 0.00,
    ///             "canBeRefunded": true,
    ///             "createdAt": "2025-10-26T14:25:00Z",
    ///             "booking": {
    ///                 "bookingId": "550e8400-e29b-41d4-a716-446655440000",
    ///                 "bookingReference": "BK20251026ABC123",
    ///                 "bookingDate": "2025-11-15T00:00:00Z",
    ///                 "status": "Confirmed",
    ///                 "activityTitle": "Scuba Diving Adventure"
    ///             }
    ///         }
    ///     }
    /// 
    /// Authorization: Customers can view their own payments, providers can view payments for their activities
    /// 
    /// </remarks>
    /// <param name="id">Payment ID</param>
    /// <returns>Payment details with booking information</returns>
    /// <response code="200">Payment details retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User not authorized to view this payment</response>
    /// <response code="404">Payment not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<PaymentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentById(Guid id)
    {
        _logger.LogInformation("Retrieving payment details for payment {PaymentId}", id);

        var query = new GetPaymentByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result.Success)
        {
            return Ok(result);
        }

        return result.Message.Contains("not found") ? NotFound(result) : BadRequest(result);
    }
}
