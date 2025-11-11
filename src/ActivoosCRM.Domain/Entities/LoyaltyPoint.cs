using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

public class LoyaltyPoint : BaseEntity
{
    private LoyaltyPoint() { }

    public Guid UserId { get; private set; }
    public virtual User User { get; private set; } = null!;

    public int Points { get; private set; }
    public string TransactionType { get; private set; } = string.Empty;
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Description { get; private set; }
    public DateTime? ExpiryDate { get; private set; }

    public static LoyaltyPoint Create(
        Guid userId,
        int points,
        string transactionType,
        string? referenceType = null,
        Guid? referenceId = null,
        string? description = null,
        int expiryDays = 365)
    {
        if (points == 0)
            throw new ArgumentException("Points cannot be zero", nameof(points));

        return new LoyaltyPoint
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Points = points,
            TransactionType = transactionType.ToLowerInvariant(),
            ReferenceType = referenceType?.ToLowerInvariant(),
            ReferenceId = referenceId,
            Description = description?.Trim(),
            ExpiryDate = transactionType == "earned" ? DateTime.UtcNow.AddDays(expiryDays) : null
        };
    }

    public bool IsExpired() => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
}