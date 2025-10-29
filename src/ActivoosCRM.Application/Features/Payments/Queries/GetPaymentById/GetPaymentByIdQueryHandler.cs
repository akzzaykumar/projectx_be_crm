using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Payments.Queries.GetPaymentById;

public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, Result<PaymentDetailsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPaymentByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PaymentDetailsDto>> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Result<PaymentDetailsDto>.CreateFailure("User not authenticated");
        }

        var payment = await _context.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b.Customer)
                    .ThenInclude(c => c.User)
            .Include(p => p.Booking)
                .ThenInclude(b => b.Activity)
                    .ThenInclude(a => a.Provider)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment == null)
        {
            return Result<PaymentDetailsDto>.CreateFailure("Payment not found");
        }

        // Check authorization - customer or provider can view
        var isCustomer = payment.Booking.Customer.UserId == currentUserId.Value;
        var isProvider = payment.Booking.Activity.Provider.UserId == currentUserId.Value;

        if (!isCustomer && !isProvider)
        {
            return Result<PaymentDetailsDto>.CreateFailure("You don't have permission to view this payment");
        }

        var dto = new PaymentDetailsDto
        {
            PaymentId = payment.Id,
            PaymentReference = payment.PaymentReference,
            BookingId = payment.BookingId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status.ToString(),
            PaymentGateway = payment.PaymentGateway,
            PaymentMethod = payment.PaymentMethod,
            GatewayTransactionId = payment.PaymentGatewayTransactionId,
            PaidAt = payment.PaidAt,
            RefundedAmount = payment.RefundedAmount,
            RefundedAt = payment.RefundedAt,
            RefundTransactionId = payment.RefundTransactionId,
            RefundReason = payment.RefundReason,
            CanBeRefunded = payment.CanBeRefunded(),
            CreatedAt = payment.CreatedAt,
            Booking = new BookingSummary
            {
                BookingId = payment.Booking.Id,
                BookingReference = payment.Booking.BookingReference,
                BookingDate = payment.Booking.BookingDate,
                Status = payment.Booking.Status.ToString(),
                ActivityTitle = payment.Booking.Activity.Title
            }
        };

        return Result<PaymentDetailsDto>.CreateSuccess(dto);
    }
}
