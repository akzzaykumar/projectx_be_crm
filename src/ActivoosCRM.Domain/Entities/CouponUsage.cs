using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// CouponUsage entity - Tracks coupon usage per booking
/// Responsible for: Recording coupon applications to bookings
/// </summary>
public class CouponUsage : BaseEntity
{
    private CouponUsage() { } // Private constructor for EF Core

    // Relationships
    public Guid CouponId { get; private set; }
    public virtual Coupon Coupon { get; private set; } = null!;

    public Guid BookingId { get; private set; }
    public virtual Booking Booking { get; private set; } = null!;

    public Guid UserId { get; private set; }
    public virtual User User { get; private set; } = null!;

    // Usage details
    public decimal DiscountAmount { get; private set; }
    public DateTime UsedAt { get; private set; }

    /// <summary>
    /// Factory method to create a new coupon usage record
    /// </summary>
    public static CouponUsage Create(
        Guid couponId,
        Guid bookingId,
        Guid userId,
        decimal discountAmount)
    {
        if (couponId == Guid.Empty)
            throw new ArgumentException("Coupon ID is required", nameof(couponId));

        if (bookingId == Guid.Empty)
            throw new ArgumentException("Booking ID is required", nameof(bookingId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        if (discountAmount < 0)
            throw new ArgumentException("Discount amount cannot be negative", nameof(discountAmount));

        var usage = new CouponUsage
        {
            Id = Guid.NewGuid(),
            CouponId = couponId,
            BookingId = bookingId,
            UserId = userId,
            DiscountAmount = discountAmount,
            UsedAt = DateTime.UtcNow
        };

        return usage;
    }
}
