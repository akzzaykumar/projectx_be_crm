using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Payments.Queries.GetPaymentMethods;

public class GetPaymentMethodsQueryHandler : IRequestHandler<GetPaymentMethodsQuery, Result<List<PaymentMethodDto>>>
{
    public Task<Result<List<PaymentMethodDto>>> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var paymentMethods = new List<PaymentMethodDto>
        {
            new PaymentMethodDto
            {
                Method = "UPI",
                DisplayName = "UPI",
                IsActive = true,
                ProcessingFee = 0.00m
            },
            new PaymentMethodDto
            {
                Method = "Card",
                DisplayName = "Credit/Debit Card",
                IsActive = true,
                ProcessingFee = 2.5m
            },
            new PaymentMethodDto
            {
                Method = "NetBanking",
                DisplayName = "Net Banking",
                IsActive = true,
                ProcessingFee = 1.5m
            },
            new PaymentMethodDto
            {
                Method = "Wallet",
                DisplayName = "Digital Wallet",
                IsActive = true,
                ProcessingFee = 1.0m
            }
        };

        return Task.FromResult(Result<List<PaymentMethodDto>>.CreateSuccess(paymentMethods));
    }
}
