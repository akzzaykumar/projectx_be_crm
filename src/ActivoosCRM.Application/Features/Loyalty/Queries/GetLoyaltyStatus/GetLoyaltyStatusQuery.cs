using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Loyalty.Queries.GetLoyaltyStatus;

/// <summary>
/// Query to get current user's loyalty status
/// </summary>
public class GetLoyaltyStatusQuery : IRequest<Result<LoyaltyStatusResponse>>
{
    // Uses current user from ICurrentUserService
}

/// <summary>
/// Response for get loyalty status query
/// </summary>
public class LoyaltyStatusResponse
{
    public LoyaltyTier CurrentTier { get; set; }
    public string CurrentTierName { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int AvailablePoints { get; set; }
    public int LifetimePoints { get; set; }
    public decimal DiscountPercentage { get; set; }
    public int PointsToNextTier { get; set; }
    public LoyaltyTier? NextTier { get; set; }
    public string? NextTierName { get; set; }
    public DateTime? TierUpgradedAt { get; set; }
    public int PointsValue { get; set; } // Points value in rupees
    public List<TierBenefitDto> TierBenefits { get; set; } = new();
}

/// <summary>
/// Tier benefit information
/// </summary>
public class TierBenefitDto
{
    public string Benefit { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Handler for GetLoyaltyStatusQuery
/// </summary>
public class GetLoyaltyStatusQueryHandler : IRequestHandler<GetLoyaltyStatusQuery, Result<LoyaltyStatusResponse>>
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetLoyaltyStatusQueryHandler> _logger;

    public GetLoyaltyStatusQueryHandler(
        ILoyaltyService loyaltyService,
        ICurrentUserService currentUserService,
        ILogger<GetLoyaltyStatusQueryHandler> logger)
    {
        _loyaltyService = loyaltyService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<LoyaltyStatusResponse>> Handle(
        GetLoyaltyStatusQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Result<LoyaltyStatusResponse>.CreateFailure("User not authenticated");
            }

            _logger.LogInformation("Getting loyalty status for user {UserId}", userId.Value);

            var loyaltyStatus = await _loyaltyService.GetUserLoyaltyStatusAsync(
                userId.Value,
                cancellationToken);

            var response = new LoyaltyStatusResponse
            {
                CurrentTier = loyaltyStatus.CurrentTier,
                CurrentTierName = loyaltyStatus.CurrentTier.ToString(),
                TotalPoints = loyaltyStatus.TotalPoints,
                AvailablePoints = loyaltyStatus.AvailablePoints,
                LifetimePoints = loyaltyStatus.LifetimePoints,
                DiscountPercentage = loyaltyStatus.DiscountPercentage,
                PointsToNextTier = loyaltyStatus.PointsToNextTier,
                NextTier = loyaltyStatus.NextTier,
                NextTierName = loyaltyStatus.NextTier?.ToString(),
                TierUpgradedAt = loyaltyStatus.TierUpgradedAt,
                PointsValue = (int)(loyaltyStatus.AvailablePoints * 0.25m), // 100 points = ₹25
                TierBenefits = GetTierBenefits(loyaltyStatus.CurrentTier)
            };

            return Result<LoyaltyStatusResponse>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loyalty status");
            return Result<LoyaltyStatusResponse>.CreateFailure(
                "Failed to retrieve loyalty status");
        }
    }

    private List<TierBenefitDto> GetTierBenefits(LoyaltyTier tier)
    {
        var benefits = new List<TierBenefitDto>();

        switch (tier)
        {
            case LoyaltyTier.Bronze:
                benefits.Add(new TierBenefitDto { Benefit = "Earn 1 point per ₹1 spent", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "50 points per review", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "Access to exclusive deals", IsActive = true });
                break;

            case LoyaltyTier.Silver:
                benefits.Add(new TierBenefitDto { Benefit = "All Bronze benefits", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "5% discount on all bookings", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "Priority customer support", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "Early access to flash sales", IsActive = true });
                break;

            case LoyaltyTier.Gold:
                benefits.Add(new TierBenefitDto { Benefit = "All Silver benefits", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "10% discount on all bookings", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "Free rescheduling", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "Birthday month special offers", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "Dedicated account manager", IsActive = true });
                break;

            case LoyaltyTier.Platinum:
                benefits.Add(new TierBenefitDto { Benefit = "All Gold benefits", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "15% discount on all bookings", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "VIP access to exclusive experiences", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "Concierge service", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "Complimentary upgrades", IsActive = true });
                benefits.Add(new TierBenefitDto { Benefit = "Partner brand discounts", IsActive = true });
                break;
        }

        return benefits;
    }
}