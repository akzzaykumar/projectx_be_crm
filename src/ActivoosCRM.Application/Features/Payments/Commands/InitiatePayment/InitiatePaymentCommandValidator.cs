using FluentValidation;

namespace ActivoosCRM.Application.Features.Payments.Commands.InitiatePayment;

public class InitiatePaymentCommandValidator : AbstractValidator<InitiatePaymentCommand>
{
    public InitiatePaymentCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("Booking ID is required");

        RuleFor(x => x.PaymentGateway)
            .NotEmpty()
            .WithMessage("Payment gateway is required")
            .Must(gateway => gateway == "Razorpay" || gateway == "Stripe" || gateway == "PayPal")
            .WithMessage("Invalid payment gateway. Supported gateways: Razorpay, Stripe, PayPal");
    }
}
