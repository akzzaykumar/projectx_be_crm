using ActivoosCRM.Domain.Common;
using ActivoosCRM.Domain.Enums;

namespace ActivoosCRM.Domain.Entities;

public class UserLoyaltyStatus : AuditableEntity
{
    private UserLoyaltyStatus() { }

    public Guid UserId { get; private set; }
    public virtual User User { get; private set; } = null!;

    public LoyaltyTier CurrentTier { get; private set; } = LoyaltyTier.Bronze;
    public int TotalPoints { get; private set; } = 0;
    public int AvailablePoints { get; private set; } = 0;
    public int LifetimePoints { get; private set; } = 0;
    public DateTime? TierUpgradedAt { get; private set; }

    public static UserLoyaltyStatus Create(Guid userId)
    {
        return new UserLoyaltyStatus
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentTier = LoyaltyTier.Bronze,
            TotalPoints = 0,
            AvailablePoints = 0,
            LifetimePoints = 0
        };
    }

    public void AddPoints(int points)
    {
        if (points <= 0)
            throw new ArgumentException("Points must be positive", nameof(points));

        TotalPoints += points;
        AvailablePoints += points;
        LifetimePoints += points;
        
        CheckAndUpgradeTier();
    }

    public void RedeemPoints(int points)
    {
        if (points <= 0)
            throw new ArgumentException("Points must be positive", nameof(points));

        if (points > AvailablePoints)
            throw new InvalidOperationException("Insufficient points");

        AvailablePoints -= points;
    }

    private void CheckAndUpgradeTier()
    {
        var newTier = TotalPoints switch
        {
            >= 50000 => LoyaltyTier.Platinum,
            >= 20000 => LoyaltyTier.Gold,
            >= 5000 => LoyaltyTier.Silver,
            _ => LoyaltyTier.Bronze
        };

        if (newTier != CurrentTier)
        {
            CurrentTier = newTier;
            TierUpgradedAt = DateTime.UtcNow;
        }
    }

    public decimal GetDiscountPercentage()
    {
        return CurrentTier switch
        {
            LoyaltyTier.Platinum => 15m,
            LoyaltyTier.Gold => 10m,
            LoyaltyTier.Silver => 5m,
            _ => 0m
        };
    }
}