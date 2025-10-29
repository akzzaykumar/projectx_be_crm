using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Payments.Queries.GetPaymentById;

public record GetPaymentByIdQuery(Guid PaymentId) : IRequest<Result<PaymentDetailsDto>>;

public record PaymentDetailsDto
{
    public Guid PaymentId { get; init; }
    public string PaymentReference { get; init; } = string.Empty;
    public Guid BookingId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PaymentGateway { get; init; } = string.Empty;
    public string? PaymentMethod { get; init; }
    public string? GatewayTransactionId { get; init; }
    public DateTime? PaidAt { get; init; }
    public decimal RefundedAmount { get; init; }
    public DateTime? RefundedAt { get; init; }
    public string? RefundTransactionId { get; init; }
    public string? RefundReason { get; init; }
    public bool CanBeRefunded { get; init; }
    public DateTime CreatedAt { get; init; }

    public BookingSummary? Booking { get; init; }
}

public record BookingSummary
{
    public Guid BookingId { get; init; }
    public string BookingReference { get; init; } = string.Empty;
    public DateTime BookingDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string ActivityTitle { get; init; } = string.Empty;
}
