using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Infrastructure.Services;

/// <summary>
/// Service for coupon validation and application
/// </summary>
public class CouponService : ICouponService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CouponService> _logger;

    public CouponService(
        IApplicationDbContext context,
        ILogger<CouponService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Validate a coupon code for a specific activity and amount
    /// </summary>
    public async Task<CouponValidationResult> ValidateCouponAsync(
        string code,
        Guid activityId,
        decimal orderAmount,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating coupon code: {Code} for activity: {ActivityId}, amount: {Amount}",
                code, activityId, orderAmount);

            // Find coupon by code
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code.ToUpperInvariant(), cancellationToken);

            if (coupon == null)
            {
                _logger.LogWarning("Coupon not found: {Code}", code);
                return CouponValidationResult.Failure("Invalid coupon code");
            }

            // Check if coupon is valid for usage
            if (!coupon.IsValidForUsage())
            {
                _logger.LogWarning("Coupon is not valid for usage: {Code}", code);
                
                if (!coupon.IsActive)
                    return CouponValidationResult.Failure("This coupon is no longer active");
                
                var now = DateTime.UtcNow;
                if (now < coupon.ValidFrom)
                    return CouponValidationResult.Failure($"This coupon is not valid until {coupon.ValidFrom:MMM dd, yyyy}");
                
                if (now > coupon.ValidUntil)
                    return CouponValidationResult.Failure("This coupon has expired");
                
                if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                    return CouponValidationResult.Failure("This coupon has reached its usage limit");
            }

            // Check if activity category is applicable
            var activity = await _context.Activities
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

            if (activity == null)
            {
                _logger.LogWarning("Activity not found: {ActivityId}", activityId);
                return CouponValidationResult.Failure("Activity not found");
            }

            if (!coupon.IsApplicableToCategory(activity.CategoryId))
            {
                _logger.LogWarning("Coupon not applicable to activity category: {Code}", code);
                return CouponValidationResult.Failure("This coupon is not applicable to this activity");
            }

            // Check minimum order amount
            if (coupon.MinOrderAmount.HasValue && orderAmount < coupon.MinOrderAmount.Value)
            {
                _logger.LogWarning("Order amount below minimum for coupon: {Code}", code);
                return CouponValidationResult.Failure(
                    $"Minimum order amount of {coupon.MinOrderAmount.Value:C} required for this coupon");
            }

            // Check if user has already used this coupon (if single-use)
            var hasUsedCoupon = await _context.CouponUsages
                .AnyAsync(cu => cu.CouponId == coupon.Id && cu.UserId == userId, cancellationToken);

            if (hasUsedCoupon && coupon.UsageLimit == 1)
            {
                _logger.LogWarning("User has already used single-use coupon: {Code}", code);
                return CouponValidationResult.Failure("You have already used this coupon");
            }

            // Calculate discount
            var discountAmount = coupon.CalculateDiscount(orderAmount);
            var discountPercentage = coupon.DiscountType == "percentage" 
                ? coupon.DiscountValue 
                : (discountAmount / orderAmount) * 100;

            _logger.LogInformation("Coupon validation successful: {Code}, discount: {Discount}",
                code, discountAmount);

            return CouponValidationResult.Success(
                coupon.Id,
                discountAmount,
                discountPercentage,
                coupon.DiscountType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating coupon: {Code}", code);
            return CouponValidationResult.Failure("An error occurred while validating the coupon");
        }
    }

    /// <summary>
    /// Apply coupon to a booking and record usage
    /// </summary>
    public async Task ApplyCouponToBookingAsync(
        Guid couponId,
        Guid bookingId,
        Guid userId,
        decimal discountAmount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Applying coupon {CouponId} to booking {BookingId}",
                couponId, bookingId);

            // Get coupon and increment usage
            var coupon = await _context.Coupons.FindAsync(new object[] { couponId }, cancellationToken);
            if (coupon == null)
            {
                _logger.LogError("Coupon not found: {CouponId}", couponId);
                throw new InvalidOperationException("Coupon not found");
            }

            // Create coupon usage record
            var couponUsage = CouponUsage.Create(
                couponId: couponId,
                userId: userId,
                bookingId: bookingId,
                discountAmount: discountAmount);

            _context.CouponUsages.Add(couponUsage);

            // Increment coupon usage count
            coupon.IncrementUsage();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully applied coupon to booking");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying coupon to booking");
            throw;
        }
    }

    /// <summary>
    /// Get all valid coupons for a user
    /// </summary>
    public async Task<List<CouponDto>> GetAvailableCouponsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Get all active coupons that are currently valid
            var coupons = await _context.Coupons
                .Where(c => c.IsActive &&
                           c.ValidFrom <= now &&
                           c.ValidUntil >= now &&
                           (!c.UsageLimit.HasValue || c.UsedCount < c.UsageLimit.Value))
                .ToListAsync(cancellationToken);

            // Filter out coupons the user has already used (if single-use)
            var userUsedCouponIds = await _context.CouponUsages
                .Where(cu => cu.UserId == userId)
                .Select(cu => cu.CouponId)
                .ToListAsync(cancellationToken);

            var availableCoupons = coupons
                .Where(c => !userUsedCouponIds.Contains(c.Id) || c.UsageLimit != 1)
                .Select(c => new CouponDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Description = c.Description,
                    DiscountType = c.DiscountType,
                    DiscountValue = c.DiscountValue,
                    MinOrderAmount = c.MinOrderAmount,
                    MaxDiscountAmount = c.MaxDiscountAmount,
                    ValidFrom = c.ValidFrom,
                    ValidUntil = c.ValidUntil,
                    UsageLimit = c.UsageLimit,
                    UsedCount = c.UsedCount,
                    IsActive = c.IsActive
                })
                .ToList();

            return availableCoupons;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available coupons for user");
            return new List<CouponDto>();
        }
    }
}