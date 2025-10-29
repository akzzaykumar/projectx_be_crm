using FluentValidation;

namespace ActivoosCRM.Application.Features.Coupons.Queries.ValidateCoupon;

public class ValidateCouponQueryValidator : AbstractValidator<ValidateCouponQuery>
{
    public ValidateCouponQueryValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Coupon code is required")
            .MaximumLength(50).WithMessage("Coupon code cannot exceed 50 characters");

        RuleFor(x => x.ActivityId)
            .NotEmpty().WithMessage("Activity ID is required");

        RuleFor(x => x.OrderAmount)
            .GreaterThan(0).WithMessage("Order amount must be greater than 0");
    }
}
