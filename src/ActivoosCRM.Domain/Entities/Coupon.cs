using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Coupon entity - Represents discount coupons for bookings
/// Responsible for: Coupon codes, discount rules, validity periods, usage tracking
/// </summary>
public class Coupon : AuditableEntity
{
    private Coupon() { } // Private constructor for EF Core

    // Coupon details
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string DiscountType { get; private set; } = string.Empty; // 'percentage', 'fixed'
    public decimal DiscountValue { get; private set; }

    // Constraints
    public decimal? MinOrderAmount { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }

    // Validity
    public DateTime ValidFrom { get; private set; }
    public DateTime ValidUntil { get; private set; }

    // Usage tracking
    public int? UsageLimit { get; private set; }
    public int UsedCount { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;

    // Applicable categories (stored as JSON array in database)
    public List<Guid> ApplicableCategories { get; private set; } = new();

    // Navigation properties
    public virtual ICollection<CouponUsage> CouponUsages { get; private set; } = new List<CouponUsage>();

    /// <summary>
    /// Factory method to create a new coupon
    /// </summary>
    public static Coupon Create(
        string code,
        string discountType,
        decimal discountValue,
        DateTime validFrom,
        DateTime validUntil,
        string? description = null,
        decimal? minOrderAmount = null,
        decimal? maxDiscountAmount = null,
        int? usageLimit = null,
        List<Guid>? applicableCategories = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        if (string.IsNullOrWhiteSpace(discountType))
            throw new ArgumentException("Discount type is required", nameof(discountType));

        if (discountValue <= 0)
            throw new ArgumentException("Discount value must be greater than 0", nameof(discountValue));

        if (validFrom >= validUntil)
            throw new ArgumentException("Valid from must be before valid until", nameof(validFrom));

        var normalizedType = discountType.ToLowerInvariant().Trim();
        if (normalizedType != "percentage" && normalizedType != "fixed")
            throw new ArgumentException("Discount type must be 'percentage' or 'fixed'", nameof(discountType));

        if (normalizedType == "percentage" && discountValue > 100)
            throw new ArgumentException("Percentage discount cannot exceed 100", nameof(discountValue));

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant().Trim(),
            Description = description?.Trim(),
            DiscountType = normalizedType,
            DiscountValue = discountValue,
            MinOrderAmount = minOrderAmount,
            MaxDiscountAmount = maxDiscountAmount,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            UsageLimit = usageLimit,
            UsedCount = 0,
            IsActive = true,
            ApplicableCategories = applicableCategories ?? new List<Guid>()
        };

        return coupon;
    }

    /// <summary>
    /// Update coupon details
    /// </summary>
    public void Update(
        string? description,
        decimal? minOrderAmount,
        decimal? maxDiscountAmount,
        int? usageLimit)
    {
        Description = description?.Trim();
        MinOrderAmount = minOrderAmount;
        MaxDiscountAmount = maxDiscountAmount;
        UsageLimit = usageLimit;
    }

    /// <summary>
    /// Update validity period
    /// </summary>
    public void UpdateValidity(DateTime validFrom, DateTime validUntil)
    {
        if (validFrom >= validUntil)
            throw new ArgumentException("Valid from must be before valid until");

        ValidFrom = validFrom;
        ValidUntil = validUntil;
    }

    /// <summary>
    /// Increment usage count
    /// </summary>
    public void IncrementUsage()
    {
        UsedCount++;
    }

    /// <summary>
    /// Activate coupon
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivate coupon
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Check if coupon is valid for usage
    /// </summary>
    public bool IsValidForUsage()
    {
        if (!IsActive) return false;

        var now = DateTime.UtcNow;
        if (now < ValidFrom || now > ValidUntil) return false;

        if (UsageLimit.HasValue && UsedCount >= UsageLimit.Value) return false;

        return true;
    }

    /// <summary>
    /// Check if coupon is applicable to category
    /// </summary>
    public bool IsApplicableToCategory(Guid categoryId)
    {
        if (!ApplicableCategories.Any()) return true; // No restrictions

        return ApplicableCategories.Contains(categoryId);
    }

    /// <summary>
    /// Calculate discount amount for given order amount
    /// </summary>
    public decimal CalculateDiscount(decimal orderAmount)
    {
        if (!IsValidForUsage())
            throw new InvalidOperationException("Coupon is not valid for usage");

        if (MinOrderAmount.HasValue && orderAmount < MinOrderAmount.Value)
            return 0;

        decimal discount = DiscountType == "percentage"
            ? orderAmount * (DiscountValue / 100)
            : DiscountValue;

        if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
            discount = MaxDiscountAmount.Value;

        return Math.Min(discount, orderAmount); // Cannot exceed order amount
    }
}
