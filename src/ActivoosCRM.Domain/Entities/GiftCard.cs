using ActivoosCRM.Domain.Common;
using ActivoosCRM.Domain.Enums;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Gift card entity for purchasing and gifting experiences
/// </summary>
public class GiftCard : AuditableEntity
{
    private GiftCard() { }

    public string Code { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "INR";
    public decimal Balance { get; private set; }
    
    public Guid? PurchasedBy { get; private set; }
    public virtual User? Purchaser { get; private set; }
    
    public string? RecipientEmail { get; private set; }
    public string? RecipientName { get; private set; }
    public string? Message { get; private set; }
    
    public GiftCardStatus Status { get; private set; } = GiftCardStatus.Active;
    public DateTime? ExpiresAt { get; private set; }
    public DateTime PurchasedAt { get; private set; }
    
    public DateTime? RedeemedAt { get; private set; }
    public Guid? RedeemedBy { get; private set; }
    public virtual User? Redeemer { get; private set; }

    public virtual ICollection<GiftCardTransaction> Transactions { get; private set; } = new List<GiftCardTransaction>();

    public static GiftCard Create(
        decimal amount,
        string currency,
        Guid? purchasedBy,
        string? recipientEmail = null,
        string? recipientName = null,
        string? message = null,
        int validityDays = 365)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));

        var code = GenerateUniqueCode();
        var expiresAt = DateTime.UtcNow.AddDays(validityDays);

        return new GiftCard
        {
            Id = Guid.NewGuid(),
            Code = code,
            Amount = amount,
            Balance = amount,
            Currency = currency.ToUpperInvariant(),
            PurchasedBy = purchasedBy,
            RecipientEmail = recipientEmail?.Trim(),
            RecipientName = recipientName?.Trim(),
            Message = message?.Trim(),
            Status = GiftCardStatus.Active,
            PurchasedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public decimal Use(decimal amount, Guid bookingId)
    {
        if (Status != GiftCardStatus.Active)
            throw new InvalidOperationException($"Gift card is {Status}");

        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
        {
            Status = GiftCardStatus.Expired;
            throw new InvalidOperationException("Gift card has expired");
        }

        if (Balance <= 0)
            throw new InvalidOperationException("Gift card has no balance");

        var amountToUse = Math.Min(amount, Balance);
        Balance -= amountToUse;

        if (Balance == 0)
        {
            Status = GiftCardStatus.Redeemed;
            RedeemedAt = DateTime.UtcNow;
        }

        return amountToUse;
    }

    public void Cancel()
    {
        if (Status == GiftCardStatus.Redeemed)
            throw new InvalidOperationException("Cannot cancel redeemed gift card");

        Status = GiftCardStatus.Cancelled;
    }

    public bool IsValid()
    {
        return Status == GiftCardStatus.Active &&
               Balance > 0 &&
               (!ExpiresAt.HasValue || ExpiresAt.Value >= DateTime.UtcNow);
    }

    private static string GenerateUniqueCode()
    {
        var random = new Random();
        var part1 = random.Next(1000, 9999);
        var part2 = random.Next(1000, 9999);
        var part3 = random.Next(1000, 9999);
        return $"FB-{part1}-{part2}-{part3}";
    }
}