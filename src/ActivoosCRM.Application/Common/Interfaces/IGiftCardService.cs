namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Service interface for gift card operations
/// </summary>
public interface IGiftCardService
{
    /// <summary>
    /// Create and purchase a new gift card
    /// </summary>
    Task<GiftCardDto> CreateGiftCardAsync(
        decimal amount,
        string currency,
        Guid purchaserId,
        string? recipientEmail = null,
        string? recipientName = null,
        string? message = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a gift card code
    /// </summary>
    Task<GiftCardValidationResult> ValidateGiftCardAsync(
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply gift card to a booking
    /// </summary>
    Task<decimal> ApplyGiftCardToBookingAsync(
        Guid bookingId,
        string code,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get gift card balance
    /// </summary>
    Task<GiftCardBalanceDto> GetGiftCardBalanceAsync(
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's gift cards (purchased or received)
    /// </summary>
    Task<List<GiftCardDto>> GetUserGiftCardsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a gift card (admin only)
    /// </summary>
    Task CancelGiftCardAsync(
        Guid giftCardId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Gift card DTO
/// </summary>
public class GiftCardDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "INR";
    public string? RecipientEmail { get; set; }
    public string? RecipientName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime PurchasedAt { get; set; }
}

/// <summary>
/// Gift card validation result
/// </summary>
public class GiftCardValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal Balance { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public static GiftCardValidationResult Success(decimal balance, DateTime? expiresAt)
    {
        return new GiftCardValidationResult
        {
            IsValid = true,
            Balance = balance,
            ExpiresAt = expiresAt
        };
    }

    public static GiftCardValidationResult Failure(string errorMessage)
    {
        return new GiftCardValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Gift card balance DTO
/// </summary>
public class GiftCardBalanceDto
{
    public string Code { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public int DaysUntilExpiry { get; set; }
}