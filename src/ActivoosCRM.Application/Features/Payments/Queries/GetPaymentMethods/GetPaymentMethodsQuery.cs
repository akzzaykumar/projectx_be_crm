using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Payments.Queries.GetPaymentMethods;

public record GetPaymentMethodsQuery : IRequest<Result<List<PaymentMethodDto>>>;

public record PaymentMethodDto
{
    public string Method { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal ProcessingFee { get; init; }
}
