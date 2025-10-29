using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Coupons.Queries.ValidateCoupon;

/// <summary>
/// Query to validate a coupon code for a specific activity and order amount
/// </summary>
public record ValidateCouponQuery(
    string Code,
    Guid ActivityId,
    decimal OrderAmount
) : IRequest<Result<CouponValidationDto>>;

/// <summary>
/// DTO containing coupon validation results
/// </summary>
public class CouponValidationDto
{
    public Guid CouponId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
}
