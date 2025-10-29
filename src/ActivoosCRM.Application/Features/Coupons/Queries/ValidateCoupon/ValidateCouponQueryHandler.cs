using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Coupons.Queries.ValidateCoupon;

public class ValidateCouponQueryHandler : IRequestHandler<ValidateCouponQuery, Result<CouponValidationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ValidateCouponQueryHandler> _logger;

    public ValidateCouponQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<ValidateCouponQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<CouponValidationDto>> Handle(ValidateCouponQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return Result<CouponValidationDto>.CreateFailure("User not authenticated");
            }

            // Get coupon by code (case-insensitive)
            var normalizedCode = request.Code.ToUpperInvariant().Trim();
            var coupon = await _context.Coupons
                .Include(c => c.CouponUsages)
                .FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);

            if (coupon == null)
            {
                return Result<CouponValidationDto>.CreateSuccess(new CouponValidationDto
                {
                    CouponId = Guid.Empty,
                    Code = request.Code,
                    IsValid = false,
                    ValidationMessage = "Invalid coupon code",
                    DiscountAmount = 0,
                    FinalAmount = request.OrderAmount
                });
            }

            // Get activity with category to check applicability
            var activity = await _context.Activities
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

            if (activity == null)
            {
                return Result<CouponValidationDto>.CreateFailure("Activity not found");
            }

            // Initialize validation DTO
            var validationDto = new CouponValidationDto
            {
                CouponId = coupon.Id,
                Code = coupon.Code,
                Description = coupon.Description,
                DiscountType = coupon.DiscountType,
                DiscountValue = coupon.DiscountValue,
                MaxDiscount = coupon.MaxDiscountAmount,
                IsValid = true,
                ValidationMessage = null,
                DiscountAmount = 0,
                FinalAmount = request.OrderAmount
            };

            // Validation 1: Check if coupon is active
            if (!coupon.IsActive)
            {
                validationDto.IsValid = false;
                validationDto.ValidationMessage = "This coupon is no longer active";
                return Result<CouponValidationDto>.CreateSuccess(validationDto);
            }

            // Validation 2: Check validity period
            var now = DateTime.UtcNow;
            if (now < coupon.ValidFrom)
            {
                validationDto.IsValid = false;
                validationDto.ValidationMessage = $"This coupon is not yet valid. Valid from {coupon.ValidFrom:yyyy-MM-dd}";
                return Result<CouponValidationDto>.CreateSuccess(validationDto);
            }

            if (now > coupon.ValidUntil)
            {
                validationDto.IsValid = false;
                validationDto.ValidationMessage = $"This coupon has expired on {coupon.ValidUntil:yyyy-MM-dd}";
                return Result<CouponValidationDto>.CreateSuccess(validationDto);
            }

            // Validation 3: Check usage limits
            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                validationDto.IsValid = false;
                validationDto.ValidationMessage = "This coupon has reached its usage limit";
                return Result<CouponValidationDto>.CreateSuccess(validationDto);
            }

            // Validation 4: Check if user has already used this coupon
            var userUsageCount = await _context.CouponUsages
                .CountAsync(cu => cu.CouponId == coupon.Id && cu.UserId == currentUserId.Value, cancellationToken);

            if (userUsageCount > 0)
            {
                validationDto.IsValid = false;
                validationDto.ValidationMessage = "You have already used this coupon";
                return Result<CouponValidationDto>.CreateSuccess(validationDto);
            }

            // Validation 5: Check minimum order amount
            if (coupon.MinOrderAmount.HasValue && request.OrderAmount < coupon.MinOrderAmount.Value)
            {
                validationDto.IsValid = false;
                validationDto.ValidationMessage = $"Minimum order amount of {coupon.MinOrderAmount.Value:C} required to use this coupon";
                return Result<CouponValidationDto>.CreateSuccess(validationDto);
            }

            // Validation 6: Check if coupon is applicable to activity's category
            if (coupon.ApplicableCategories.Any() && !coupon.ApplicableCategories.Contains(activity.CategoryId))
            {
                validationDto.IsValid = false;
                validationDto.ValidationMessage = "This coupon is not applicable to the selected activity";
                return Result<CouponValidationDto>.CreateSuccess(validationDto);
            }

            // All validations passed - calculate discount
            decimal discountAmount = 0;
            if (coupon.DiscountType == "percentage")
            {
                discountAmount = request.OrderAmount * (coupon.DiscountValue / 100);

                // Apply max discount cap for percentage discounts
                if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
                {
                    discountAmount = coupon.MaxDiscountAmount.Value;
                }
            }
            else if (coupon.DiscountType == "fixed")
            {
                discountAmount = coupon.DiscountValue;
            }

            // Discount cannot exceed order amount
            discountAmount = Math.Min(discountAmount, request.OrderAmount);

            // Round to 2 decimal places
            discountAmount = Math.Round(discountAmount, 2);

            validationDto.DiscountAmount = discountAmount;
            validationDto.FinalAmount = request.OrderAmount - discountAmount;
            validationDto.ValidationMessage = "Coupon is valid and can be applied";

            _logger.LogInformation(
                "Coupon {CouponCode} validated successfully for user {UserId}. Discount: {DiscountAmount}, Final: {FinalAmount}",
                coupon.Code, currentUserId.Value, discountAmount, validationDto.FinalAmount);

            return Result<CouponValidationDto>.CreateSuccess(validationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating coupon {CouponCode}", request.Code);
            return Result<CouponValidationDto>.CreateFailure("Failed to validate coupon");
        }
    }
}
