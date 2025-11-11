namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Service interface for coupon validation and application
/// </summary>
public interface ICouponService
{
    /// <summary>
    /// Validate a coupon code for a specific activity and amount
    /// </summary>
    /// <param name="code">Coupon code</param>
    /// <param name="activityId">Activity ID</param>
    /// <param name="orderAmount">Order amount</param>
    /// <param name="userId">User ID applying the coupon</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Coupon validation result</returns>
    Task<CouponValidationResult> ValidateCouponAsync(
        string code, 
        Guid activityId, 
        decimal orderAmount, 
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply coupon to a booking and record usage
    /// </summary>
    /// <param name="couponId">Coupon ID</param>
    /// <param name="bookingId">Booking ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="discountAmount">Calculated discount amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ApplyCouponToBookingAsync(
        Guid couponId, 
        Guid bookingId, 
        Guid userId, 
        decimal discountAmount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all valid coupons for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<CouponDto>> GetAvailableCouponsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of coupon validation
/// </summary>
public class CouponValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CouponId { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string? DiscountType { get; set; }

    public static CouponValidationResult Success(
        Guid couponId, 
        decimal discountAmount, 
        decimal discountPercentage,
        string discountType)
    {
        return new CouponValidationResult
        {
            IsValid = true,
            CouponId = couponId,
            DiscountAmount = discountAmount,
            DiscountPercentage = discountPercentage,
            DiscountType = discountType
        };
    }

    public static CouponValidationResult Failure(string errorMessage)
    {
        return new CouponValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// DTO for coupon information
/// </summary>
public class CouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
}