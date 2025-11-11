using ActivoosCRM.Domain.Enums;

namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Service interface for loyalty points management
/// </summary>
public interface ILoyaltyService
{
    /// <summary>
    /// Award points to user for an action
    /// </summary>
    Task AwardPointsAsync(
        Guid userId,
        int points,
        string transactionType,
        string? referenceType = null,
        Guid? referenceId = null,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Redeem points for a discount
    /// </summary>
    Task<decimal> RedeemPointsAsync(
        Guid userId,
        int points,
        Guid bookingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's loyalty status and available points
    /// </summary>
    Task<LoyaltyStatusDto> GetUserLoyaltyStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's loyalty transaction history
    /// </summary>
    Task<List<LoyaltyTransactionDto>> GetLoyaltyHistoryAsync(
        Guid userId,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Award points for booking completion
    /// </summary>
    Task AwardBookingPointsAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Award points for review submission
    /// </summary>
    Task AwardReviewPointsAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate loyalty discount for booking
    /// </summary>
    Task<decimal> CalculateLoyaltyDiscountAsync(
        Guid userId,
        decimal bookingAmount,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Loyalty status DTO
/// </summary>
public class LoyaltyStatusDto
{
    public LoyaltyTier CurrentTier { get; set; }
    public int TotalPoints { get; set; }
    public int AvailablePoints { get; set; }
    public int LifetimePoints { get; set; }
    public decimal DiscountPercentage { get; set; }
    public int PointsToNextTier { get; set; }
    public LoyaltyTier? NextTier { get; set; }
    public DateTime? TierUpgradedAt { get; set; }
}

/// <summary>
/// Loyalty transaction DTO
/// </summary>
public class LoyaltyTransactionDto
{
    public Guid Id { get; set; }
    public int Points { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
}