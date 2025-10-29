using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Coupons.Queries.GetMyCoupons;

public class GetMyCouponsQueryHandler : IRequestHandler<GetMyCouponsQuery, Result<List<UserCouponDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetMyCouponsQueryHandler> _logger;

    public GetMyCouponsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetMyCouponsQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<List<UserCouponDto>>> Handle(GetMyCouponsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return Result<List<UserCouponDto>>.CreateFailure("User not authenticated");
            }

            var now = DateTime.UtcNow;

            // Get all active coupons (no specific user restrictions in current schema)
            // In a more advanced system, you could filter by user-specific coupons
            var coupons = await _context.Coupons
                .Include(c => c.CouponUsages)
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

            // Get user's coupon usage history
            var userCouponUsages = await _context.CouponUsages
                .Where(cu => cu.UserId == currentUserId.Value)
                .Select(cu => cu.CouponId)
                .ToListAsync(cancellationToken);

            var couponDtos = coupons.Select(coupon =>
            {
                var dto = new UserCouponDto
                {
                    CouponId = coupon.Id,
                    Code = coupon.Code,
                    Description = coupon.Description,
                    DiscountType = coupon.DiscountType,
                    DiscountValue = coupon.DiscountValue,
                    ValidFrom = coupon.ValidFrom,
                    ValidUntil = coupon.ValidUntil,
                    MinOrderAmount = coupon.MinOrderAmount,
                    MaxDiscountAmount = coupon.MaxDiscountAmount,
                    IsActive = coupon.IsActive,
                    UsageLimit = coupon.UsageLimit,
                    UsedCount = coupon.UsedCount,
                    CanUse = true,
                    CannotUseReason = null
                };

                // Check if user can use this coupon
                if (!coupon.IsActive)
                {
                    dto.CanUse = false;
                    dto.CannotUseReason = "Coupon is no longer active";
                }
                else if (now < coupon.ValidFrom)
                {
                    dto.CanUse = false;
                    dto.CannotUseReason = $"Not yet valid. Valid from {coupon.ValidFrom:yyyy-MM-dd}";
                }
                else if (now > coupon.ValidUntil)
                {
                    dto.CanUse = false;
                    dto.CannotUseReason = $"Expired on {coupon.ValidUntil:yyyy-MM-dd}";
                }
                else if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                {
                    dto.CanUse = false;
                    dto.CannotUseReason = "Usage limit reached";
                }
                else if (userCouponUsages.Contains(coupon.Id))
                {
                    dto.CanUse = false;
                    dto.CannotUseReason = "Already used by you";
                }

                return dto;
            }).ToList();

            _logger.LogInformation(
                "Retrieved {CouponCount} coupons for user {UserId} ({UsableCount} usable)",
                couponDtos.Count, currentUserId.Value, couponDtos.Count(c => c.CanUse));

            return Result<List<UserCouponDto>>.CreateSuccess(couponDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving coupons for current user");
            return Result<List<UserCouponDto>>.CreateFailure("Failed to retrieve coupons");
        }
    }
}
