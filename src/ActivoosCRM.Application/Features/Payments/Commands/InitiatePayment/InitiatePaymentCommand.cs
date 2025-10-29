using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Payments.Commands.InitiatePayment;

public record InitiatePaymentCommand(
    Guid BookingId,
    string PaymentGateway = "Razorpay"
) : IRequest<Result<InitiatePaymentResponse>>;

public record InitiatePaymentResponse
{
    public Guid PaymentId { get; init; }
    public string PaymentReference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string GatewayOrderId { get; init; } = string.Empty;
    public string GatewayKey { get; init; } = string.Empty;
    public string CallbackUrl { get; init; } = string.Empty;
}
