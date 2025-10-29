using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using ActivoosCRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Payments.Commands.InitiatePayment;

public class InitiatePaymentCommandHandler : IRequestHandler<InitiatePaymentCommand, Result<InitiatePaymentResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;
    private readonly IRazorpayService _razorpayService;
    private readonly ILogger<InitiatePaymentCommandHandler> _logger;

    public InitiatePaymentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IConfiguration configuration,
        IRazorpayService razorpayService,
        ILogger<InitiatePaymentCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _configuration = configuration;
        _razorpayService = razorpayService;
        _logger = logger;
    }

    public async Task<Result<InitiatePaymentResponse>> Handle(InitiatePaymentCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Result<InitiatePaymentResponse>.CreateFailure("User not authenticated");
        }

        // Get booking with customer profile
        var booking = await _context.Bookings
            .Include(b => b.Customer)
                .ThenInclude(c => c.User)
            .Include(b => b.Activity)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            return Result<InitiatePaymentResponse>.CreateFailure("Booking not found");
        }

        // Verify ownership
        if (booking.Customer.UserId != currentUserId.Value)
        {
            return Result<InitiatePaymentResponse>.CreateFailure("You can only initiate payment for your own bookings");
        }

        // Check if booking already has a completed payment
        var existingPayment = await _context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == booking.Id && p.Status == PaymentStatus.Completed, cancellationToken);

        if (existingPayment != null)
        {
            return Result<InitiatePaymentResponse>.CreateFailure("This booking has already been paid");
        }

        // Check booking status
        if (booking.Status == BookingStatus.Cancelled)
        {
            return Result<InitiatePaymentResponse>.CreateFailure("Cannot pay for a cancelled booking");
        }

        // Create or update payment record
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == booking.Id && p.Status == PaymentStatus.Pending, cancellationToken);

        if (payment == null)
        {
            payment = Payment.Create(
                booking.Id,
                booking.TotalAmount,
                request.PaymentGateway,
                booking.Currency);

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);
        }

        try
        {
            // Create order in Razorpay
            string gatewayOrderId;
            if (request.PaymentGateway.Equals("Razorpay", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Creating Razorpay order for booking {BookingReference}", booking.BookingReference);

                var razorpayOrder = await _razorpayService.CreateOrderAsync(
                    payment.Amount,
                    payment.Currency,
                    booking.BookingReference,
                    cancellationToken);

                gatewayOrderId = razorpayOrder.OrderId;

                _logger.LogInformation("Razorpay order created: {OrderId} for payment {PaymentId}",
                    gatewayOrderId, payment.Id);
            }
            else
            {
                // Fallback for other payment gateways (not implemented yet)
                gatewayOrderId = $"order_{Guid.NewGuid().ToString("N")[..16]}";
                _logger.LogWarning("Payment gateway {Gateway} not fully implemented, using mock order ID",
                    request.PaymentGateway);
            }

            payment.SetGatewayOrderId(gatewayOrderId);
            await _context.SaveChangesAsync(cancellationToken);

            // Get gateway configuration
            var gatewayKey = _configuration["Razorpay:KeyId"]
                ?? Environment.GetEnvironmentVariable("RAZORPAY_KEY_ID")
                ?? "rzp_test_xxxxxxxxxx";

            var callbackUrl = _configuration["PaymentGateway:CallbackUrl"]
                ?? Environment.GetEnvironmentVariable("PAYMENT_GATEWAY_CALLBACK_URL")
                ?? "https://api.funbookr.com/api/bookings/webhook/payment";

            var response = new InitiatePaymentResponse
            {
                PaymentId = payment.Id,
                PaymentReference = payment.PaymentReference,
                Amount = payment.Amount,
                Currency = payment.Currency,
                GatewayOrderId = gatewayOrderId,
                GatewayKey = gatewayKey,
                CallbackUrl = callbackUrl
            };

            return Result<InitiatePaymentResponse>.CreateSuccess(response, "Payment initiated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment for booking {BookingId}", booking.Id);
            return Result<InitiatePaymentResponse>.CreateFailure("Failed to initiate payment. Please try again.");
        }
    }
}
