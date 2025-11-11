using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Domain.Entities;
using ActivoosCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Infrastructure.Services;

/// <summary>
/// Service for loyalty points management and tier system
/// </summary>
public class LoyaltyService : ILoyaltyService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<LoyaltyService> _logger;

    // Points earning rules
    private const int POINTS_PER_BOOKING_RUPEE = 1; // 1 point per ₹1 spent
    private const int POINTS_FOR_REVIEW = 50;
    private const int POINTS_FOR_PHOTO_REVIEW = 100;
    private const int POINTS_FOR_REFERRAL = 500;
    private const int POINTS_FOR_FIRST_BOOKING = 250;

    // Points redemption rate
    private const decimal POINTS_TO_RUPEES_RATIO = 0.25m; // 100 points = ₹25

    public LoyaltyService(
        IApplicationDbContext context,
        ILogger<LoyaltyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Award points to user for an action
    /// </summary>
    public async Task AwardPointsAsync(
        Guid userId,
        int points,
        string transactionType,
        string? referenceType = null,
        Guid? referenceId = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Awarding {Points} points to user {UserId} for {Type}",
                points, userId, transactionType);

            // Get or create user loyalty status
            var loyaltyStatus = await _context.UserLoyaltyStatuses
                .FirstOrDefaultAsync(ls => ls.UserId == userId, cancellationToken);

            if (loyaltyStatus == null)
            {
                loyaltyStatus = UserLoyaltyStatus.Create(userId);
                _context.UserLoyaltyStatuses.Add(loyaltyStatus);
            }

            // Add points to user's status
            loyaltyStatus.AddPoints(points);

            // Create loyalty point transaction
            var transaction = LoyaltyPoint.Create(
                userId,
                points,
                transactionType,
                referenceType,
                referenceId,
                description);

            _context.LoyaltyPoints.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Points awarded successfully. User now has {TotalPoints} points (Tier: {Tier})",
                loyaltyStatus.TotalPoints,
                loyaltyStatus.CurrentTier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding points to user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Redeem points for a discount on booking
    /// </summary>
    public async Task<decimal> RedeemPointsAsync(
        Guid userId,
        int points,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Redeeming {Points} points for user {UserId} on booking {BookingId}",
                points, userId, bookingId);

            // Get user loyalty status
            var loyaltyStatus = await _context.UserLoyaltyStatuses
                .FirstOrDefaultAsync(ls => ls.UserId == userId, cancellationToken);

            if (loyaltyStatus == null)
                throw new InvalidOperationException("User has no loyalty points");

            if (points <= 0)
                throw new ArgumentException("Points must be positive", nameof(points));

            if (points > loyaltyStatus.AvailablePoints)
                throw new InvalidOperationException($"Insufficient points. Available: {loyaltyStatus.AvailablePoints}");

            // Validate minimum redemption
            if (points < 100)
                throw new InvalidOperationException("Minimum 100 points required for redemption");

            // Calculate discount amount
            var discountAmount = points * POINTS_TO_RUPEES_RATIO;

            // Redeem points from user's status
            loyaltyStatus.RedeemPoints(points);

            // Create redemption transaction
            var transaction = LoyaltyPoint.Create(
                userId,
                -points, // Negative for redemption
                "redeemed",
                "booking",
                bookingId,
                $"Redeemed {points} points for ₹{discountAmount:N2} discount");

            _context.LoyaltyPoints.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Points redeemed successfully. Discount amount: ₹{Discount}, Remaining points: {Points}",
                discountAmount,
                loyaltyStatus.AvailablePoints);

            return discountAmount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redeeming points for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get user's loyalty status
    /// </summary>
    public async Task<LoyaltyStatusDto> GetUserLoyaltyStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var loyaltyStatus = await _context.UserLoyaltyStatuses
                .FirstOrDefaultAsync(ls => ls.UserId == userId, cancellationToken);

            if (loyaltyStatus == null)
            {
                // Return default status for new users
                return new LoyaltyStatusDto
                {
                    CurrentTier = LoyaltyTier.Bronze,
                    TotalPoints = 0,
                    AvailablePoints = 0,
                    LifetimePoints = 0,
                    DiscountPercentage = 0,
                    PointsToNextTier = 5000,
                    NextTier = LoyaltyTier.Silver
                };
            }

            var nextTier = GetNextTier(loyaltyStatus.CurrentTier);
            var pointsToNextTier = nextTier.HasValue
                ? GetPointsRequiredForTier(nextTier.Value) - loyaltyStatus.TotalPoints
                : 0;

            return new LoyaltyStatusDto
            {
                CurrentTier = loyaltyStatus.CurrentTier,
                TotalPoints = loyaltyStatus.TotalPoints,
                AvailablePoints = loyaltyStatus.AvailablePoints,
                LifetimePoints = loyaltyStatus.LifetimePoints,
                DiscountPercentage = loyaltyStatus.GetDiscountPercentage(),
                PointsToNextTier = pointsToNextTier,
                NextTier = nextTier,
                TierUpgradedAt = loyaltyStatus.TierUpgradedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loyalty status for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get user's loyalty transaction history
    /// </summary>
    public async Task<List<LoyaltyTransactionDto>> GetLoyaltyHistoryAsync(
        Guid userId,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transactions = await _context.LoyaltyPoints
                .Where(lp => lp.UserId == userId)
                .OrderByDescending(lp => lp.CreatedAt)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return transactions.Select(t => new LoyaltyTransactionDto
            {
                Id = t.Id,
                Points = t.Points,
                TransactionType = t.TransactionType,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                ExpiryDate = t.ExpiryDate,
                IsExpired = t.IsExpired()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loyalty history for user {UserId}", userId);
            return new List<LoyaltyTransactionDto>();
        }
    }

    /// <summary>
    /// Award points for booking completion
    /// </summary>
    public async Task AwardBookingPointsAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null)
            {
                _logger.LogWarning("Booking not found: {BookingId}", bookingId);
                return;
            }

            // Check if this is user's first booking
            var isFirstBooking = !await _context.Bookings
                .AnyAsync(b => 
                    b.CustomerId == booking.CustomerId && 
                    b.Id != bookingId && 
                    b.Status == BookingStatus.Completed,
                    cancellationToken);

            // Calculate points (1 point per rupee)
            var bookingPoints = (int)Math.Floor(booking.TotalAmount * POINTS_PER_BOOKING_RUPEE);

            // Bonus points for first booking
            if (isFirstBooking)
            {
                bookingPoints += POINTS_FOR_FIRST_BOOKING;
                
                await AwardPointsAsync(
                    booking.Customer.UserId,
                    POINTS_FOR_FIRST_BOOKING,
                    "earned",
                    "first_booking_bonus",
                    bookingId,
                    "First booking bonus",
                    cancellationToken);
            }

            // Award booking points
            await AwardPointsAsync(
                booking.Customer.UserId,
                bookingPoints,
                "earned",
                "booking",
                bookingId,
                $"Booking completed - {booking.Activity?.Title ?? "Activity"}",
                cancellationToken);

            _logger.LogInformation(
                "Awarded {Points} points for booking {BookingId}",
                bookingPoints + (isFirstBooking ? POINTS_FOR_FIRST_BOOKING : 0),
                bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding booking points");
        }
    }

    /// <summary>
    /// Award points for review submission
    /// </summary>
    public async Task AwardReviewPointsAsync(
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var review = await _context.Reviews
                .Include(r => r.Booking)
                .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);

            if (review == null)
            {
                _logger.LogWarning("Review not found: {ReviewId}", reviewId);
                return;
            }

            var userId = review.Booking.Customer.UserId;
            
            // Award more points for detailed reviews
            var points = !string.IsNullOrEmpty(review.ReviewText) && review.ReviewText.Length > 100
                ? POINTS_FOR_PHOTO_REVIEW
                : POINTS_FOR_REVIEW;

            await AwardPointsAsync(
                userId,
                points,
                "earned",
                "review",
                reviewId,
                $"Review submitted for {review.Activity?.Title ?? "activity"}",
                cancellationToken);

            _logger.LogInformation(
                "Awarded {Points} points for review {ReviewId}",
                points, reviewId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding review points");
        }
    }

    /// <summary>
    /// Calculate loyalty discount for booking
    /// </summary>
    public async Task<decimal> CalculateLoyaltyDiscountAsync(
        Guid userId,
        decimal bookingAmount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var loyaltyStatus = await _context.UserLoyaltyStatuses
                .FirstOrDefaultAsync(ls => ls.UserId == userId, cancellationToken);

            if (loyaltyStatus == null)
                return 0;

            var discountPercentage = loyaltyStatus.GetDiscountPercentage();
            var discountAmount = bookingAmount * (discountPercentage / 100);

            return Math.Round(discountAmount, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating loyalty discount");
            return 0;
        }
    }

    /// <summary>
    /// Get next tier in loyalty progression
    /// </summary>
    private LoyaltyTier? GetNextTier(LoyaltyTier currentTier)
    {
        return currentTier switch
        {
            LoyaltyTier.Bronze => LoyaltyTier.Silver,
            LoyaltyTier.Silver => LoyaltyTier.Gold,
            LoyaltyTier.Gold => LoyaltyTier.Platinum,
            LoyaltyTier.Platinum => null,
            _ => null
        };
    }

    /// <summary>
    /// Get points required to reach a tier
    /// </summary>
    private int GetPointsRequiredForTier(LoyaltyTier tier)
    {
        return tier switch
        {
            LoyaltyTier.Bronze => 0,
            LoyaltyTier.Silver => 5000,
            LoyaltyTier.Gold => 20000,
            LoyaltyTier.Platinum => 50000,
            _ => 0
        };
    }
}