using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Bookings.Commands.CancelBooking;

public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, Result<CancelBookingResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRazorpayService _razorpayService;
    private readonly ILogger<CancelBookingCommandHandler> _logger;

    public CancelBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IRazorpayService razorpayService,
        ILogger<CancelBookingCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _razorpayService = razorpayService;
        _logger = logger;
    }

    public async Task<Result<CancelBookingResponse>> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Result<CancelBookingResponse>.CreateFailure("User not authenticated");
        }

        // Get customer profile
        var customerProfile = await _context.CustomerProfiles
            .FirstOrDefaultAsync(c => c.UserId == currentUserId, cancellationToken);

        if (customerProfile == null)
        {
            return Result<CancelBookingResponse>.CreateFailure("Customer profile not found");
        }

        // Get booking with activity and payment
        var booking = await _context.Bookings
            .Include(b => b.Activity)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            return Result<CancelBookingResponse>.CreateFailure("Booking not found");
        }

        // Verify ownership
        if (booking.CustomerId != customerProfile.Id)
        {
            return Result<CancelBookingResponse>.CreateFailure("You do not have permission to cancel this booking");
        }

        // Check if booking can be cancelled
        if (!booking.CanBeCancelled)
        {
            return Result<CancelBookingResponse>.CreateFailure(
                $"Booking cannot be cancelled. Current status: {booking.Status}");
        }

        // Calculate hours until booking
        var hoursUntilBooking = booking.GetHoursUntilBooking();

        // Determine refund amount based on cancellation policy
        // Policy: 
        // - 48+ hours: 100% refund
        // - 24-48 hours: 50% refund  
        // - Less than 24 hours: No refund
        decimal refundAmount = 0;
        string refundStatus = "No Refund";

        if (booking.IsPaid && booking.Payment != null)
        {
            if (hoursUntilBooking >= 48)
            {
                refundAmount = booking.TotalAmount;
                refundStatus = "Full Refund";
            }
            else if (hoursUntilBooking >= 24)
            {
                refundAmount = booking.TotalAmount * 0.5m;
                refundStatus = "Partial Refund (50%)";
            }
            else
            {
                refundAmount = 0;
                refundStatus = "No Refund (Less than 24 hours)";
            }
        }
        else
        {
            refundStatus = "Not Paid - No Refund Required";
        }

        // Cancel the booking
        booking.Cancel(currentUserId.Value, request.CancellationReason);

        // Process refund if applicable
        if (refundAmount > 0 && booking.Payment != null)
        {
            booking.ProcessRefund(refundAmount);

            // Process refund through payment gateway
            if (refundAmount >= booking.TotalAmount)
            {
                // TODO: Get actual refund transaction ID from payment gateway API call
                booking.Payment.ProcessFullRefund("REFUND_TXN_" + Guid.NewGuid().ToString("N")[..16], "Cancelled by customer");
            }
            else
            {
                // TODO: Get actual refund transaction ID from payment gateway API call
                booking.Payment.ProcessPartialRefund(refundAmount, "REFUND_TXN_" + Guid.NewGuid().ToString("N")[..16], "Partial refund - cancelled by customer");
            }

            // TODO: Initiate actual refund with payment gateway (Razorpay)
            // This would involve calling the payment gateway API to process the refund
        }

        await _context.SaveChangesAsync(cancellationToken);

        var response = new CancelBookingResponse
        {
            BookingId = booking.Id,
            Status = booking.Status.ToString(),
            RefundAmount = refundAmount,
            RefundStatus = refundStatus,
            Message = refundAmount > 0
                ? $"Booking cancelled successfully. {refundStatus} of ₹{refundAmount:N2} will be processed within 5-7 business days."
                : "Booking cancelled successfully. No refund applicable."
        };

        return Result<CancelBookingResponse>.CreateSuccess(response, "Booking cancelled successfully");
    }
}
