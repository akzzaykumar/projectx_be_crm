using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Coupons.Queries.GetMyCoupons;

/// <summary>
/// Query to get user's available coupons
/// </summary>
public record GetMyCouponsQuery() : IRequest<Result<List<UserCouponDto>>>;

/// <summary>
/// DTO containing user's coupon details
/// </summary>
public class UserCouponDto
{
    public Guid CouponId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public bool IsActive { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool CanUse { get; set; }
    public string? CannotUseReason { get; set; }
}
