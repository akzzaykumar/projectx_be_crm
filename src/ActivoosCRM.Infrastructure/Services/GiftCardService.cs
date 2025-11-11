using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Domain.Entities;
using ActivoosCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Infrastructure.Services;

/// <summary>
/// Service for gift card management and operations
/// </summary>
public class GiftCardService : IGiftCardService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<GiftCardService> _logger;

    public GiftCardService(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<GiftCardService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Create and purchase a new gift card
    /// </summary>
    public async Task<GiftCardDto> CreateGiftCardAsync(
        decimal amount,
        string currency,
        Guid purchaserId,
        string? recipientEmail = null,
        string? recipientName = null,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating gift card. Amount: {Amount}, Purchaser: {PurchaserId}",
                amount, purchaserId);

            // Validate amount
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than 0", nameof(amount));

            if (amount < 500)
                throw new ArgumentException("Minimum gift card amount is ₹500", nameof(amount));

            if (amount > 50000)
                throw new ArgumentException("Maximum gift card amount is ₹50,000", nameof(amount));

            // Validate purchaser exists
            var purchaser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == purchaserId && !u.IsDeleted, cancellationToken);

            if (purchaser == null)
                throw new InvalidOperationException("Purchaser not found");

            // Create gift card
            var giftCard = GiftCard.Create(
                amount,
                currency,
                purchaserId,
                recipientEmail,
                recipientName,
                message,
                validityDays: 365); // 1 year validity

            _context.GiftCards.Add(giftCard);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Gift card created successfully. Code: {Code}", giftCard.Code);

            // Send email to recipient if email provided
            if (!string.IsNullOrEmpty(recipientEmail))
            {
                try
                {
                    await SendGiftCardEmailAsync(giftCard, purchaser, cancellationToken);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send gift card email to: {Email}", recipientEmail);
                    // Don't fail gift card creation if email fails
                }
            }

            return MapToDto(giftCard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gift card");
            throw;
        }
    }

    /// <summary>
    /// Validate a gift card code
    /// </summary>
    public async Task<GiftCardValidationResult> ValidateGiftCardAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedCode = code.ToUpperInvariant().Trim();

            var giftCard = await _context.GiftCards
                .FirstOrDefaultAsync(gc => gc.Code == normalizedCode, cancellationToken);

            if (giftCard == null)
            {
                _logger.LogWarning("Gift card not found: {Code}", code);
                return GiftCardValidationResult.Failure("Invalid gift card code");
            }

            if (giftCard.Status == GiftCardStatus.Cancelled)
                return GiftCardValidationResult.Failure("This gift card has been cancelled");

            if (giftCard.Status == GiftCardStatus.Expired)
                return GiftCardValidationResult.Failure("This gift card has expired");

            if (giftCard.Status == GiftCardStatus.Redeemed && giftCard.Balance <= 0)
                return GiftCardValidationResult.Failure("This gift card has been fully redeemed");

            if (giftCard.ExpiresAt.HasValue && giftCard.ExpiresAt.Value < DateTime.UtcNow)
            {
                giftCard.Cancel(); // Auto-expire
                await _context.SaveChangesAsync(cancellationToken);
                return GiftCardValidationResult.Failure("This gift card has expired");
            }

            if (giftCard.Balance <= 0)
                return GiftCardValidationResult.Failure("This gift card has no remaining balance");

            return GiftCardValidationResult.Success(giftCard.Balance, giftCard.ExpiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating gift card: {Code}", code);
            return GiftCardValidationResult.Failure("An error occurred while validating the gift card");
        }
    }

    /// <summary>
    /// Apply gift card to a booking
    /// </summary>
    public async Task<decimal> ApplyGiftCardToBookingAsync(
        Guid bookingId,
        string code,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Applying gift card {Code} to booking {BookingId}",
                code, bookingId);

            var normalizedCode = code.ToUpperInvariant().Trim();

            // Validate gift card
            var validation = await ValidateGiftCardAsync(normalizedCode, cancellationToken);
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.ErrorMessage);

            // Get gift card
            var giftCard = await _context.GiftCards
                .FirstOrDefaultAsync(gc => gc.Code == normalizedCode, cancellationToken);

            if (giftCard == null)
                throw new InvalidOperationException("Gift card not found");

            // Get booking
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null)
                throw new InvalidOperationException("Booking not found");

            // Verify user owns the booking
            if (booking.Customer.UserId != userId)
                throw new InvalidOperationException("You can only apply gift cards to your own bookings");

            // Check if booking already paid
            if (booking.IsPaid)
                throw new InvalidOperationException("Cannot apply gift card to already paid booking");

            // Calculate amount to apply
            var amountToApply = giftCard.Use(booking.TotalAmount, bookingId);

            // Create transaction record
            var transaction = GiftCardTransaction.Create(
                giftCard.Id,
                bookingId,
                amountToApply,
                giftCard.Balance);

            _context.GiftCardTransactions.Add(transaction);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Gift card applied successfully. Amount: {Amount}, Remaining balance: {Balance}",
                amountToApply, giftCard.Balance);

            return amountToApply;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying gift card to booking");
            throw;
        }
    }

    /// <summary>
    /// Get gift card balance
    /// </summary>
    public async Task<GiftCardBalanceDto> GetGiftCardBalanceAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedCode = code.ToUpperInvariant().Trim();

            var giftCard = await _context.GiftCards
                .FirstOrDefaultAsync(gc => gc.Code == normalizedCode, cancellationToken);

            if (giftCard == null)
                throw new InvalidOperationException("Gift card not found");

            var daysUntilExpiry = giftCard.ExpiresAt.HasValue
                ? (int)(giftCard.ExpiresAt.Value - DateTime.UtcNow).TotalDays
                : int.MaxValue;

            return new GiftCardBalanceDto
            {
                Code = giftCard.Code,
                Balance = giftCard.Balance,
                Currency = giftCard.Currency,
                ExpiresAt = giftCard.ExpiresAt,
                IsExpired = giftCard.ExpiresAt.HasValue && giftCard.ExpiresAt.Value < DateTime.UtcNow,
                DaysUntilExpiry = Math.Max(0, daysUntilExpiry)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gift card balance");
            throw;
        }
    }

    /// <summary>
    /// Get user's gift cards
    /// </summary>
    public async Task<List<GiftCardDto>> GetUserGiftCardsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get gift cards purchased by user or sent to their email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                return new List<GiftCardDto>();

            var giftCards = await _context.GiftCards
                .Where(gc => gc.PurchasedBy == userId || gc.RecipientEmail == user.Email)
                .OrderByDescending(gc => gc.PurchasedAt)
                .ToListAsync(cancellationToken);

            return giftCards.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user gift cards");
            return new List<GiftCardDto>();
        }
    }

    /// <summary>
    /// Cancel a gift card
    /// </summary>
    public async Task CancelGiftCardAsync(
        Guid giftCardId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var giftCard = await _context.GiftCards
                .FirstOrDefaultAsync(gc => gc.Id == giftCardId, cancellationToken);

            if (giftCard == null)
                throw new InvalidOperationException("Gift card not found");

            giftCard.Cancel();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Gift card cancelled: {Code}", giftCard.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling gift card");
            throw;
        }
    }

    /// <summary>
    /// Send gift card email to recipient
    /// </summary>
    private async Task SendGiftCardEmailAsync(
        GiftCard giftCard,
        User purchaser,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(giftCard.RecipientEmail))
            return;

        var emailHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ padding: 30px; background-color: #f9f9f9; }}
        .gift-card {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 30px; border-radius: 10px; color: white; text-align: center; margin: 20px 0; }}
        .code {{ font-size: 28px; font-weight: bold; letter-spacing: 4px; background-color: rgba(255,255,255,0.2); padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .amount {{ font-size: 36px; font-weight: bold; margin: 10px 0; }}
        .footer {{ padding: 20px; font-size: 12px; color: #666; text-align: center; }}
        .button {{ display: inline-block; padding: 15px 30px; background-color: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎁 You've Received a Gift Card!</h1>
        </div>
        <div class='content'>
            <p>Hello {giftCard.RecipientName ?? "there"},</p>
            <p>{purchaser.FirstName} {purchaser.LastName} has sent you a FunBookr gift card!</p>
            
            {(!string.IsNullOrEmpty(giftCard.Message) 
                ? $"<div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0;'><p><strong>Personal Message:</strong></p><p style='font-style: italic;'>\"{giftCard.Message}\"</p></div>" 
                : "")}
            
            <div class='gift-card'>
                <h2>Your Gift Card</h2>
                <div class='amount'>{giftCard.Currency} {giftCard.Amount:N2}</div>
                <p>Gift Card Code:</p>
                <div class='code'>{giftCard.Code}</div>
                <p style='font-size: 14px; margin-top: 20px;'>Valid until: {giftCard.ExpiresAt:MMMM dd, yyyy}</p>
            </div>
            
            <h3>How to Use:</h3>
            <ol>
                <li>Browse and select your favorite activity on FunBookr</li>
                <li>Proceed to checkout</li>
                <li>Enter your gift card code: <strong>{giftCard.Code}</strong></li>
                <li>The amount will be applied to your booking!</li>
            </ol>
            
            <p style='text-align: center;'>
                <a href='https://app.funbookr.com/activities' class='button'>Start Exploring Activities</a>
            </p>
            
            <div style='background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                <p><strong>💡 Gift Card Tips:</strong></p>
                <ul>
                    <li>Your gift card can be used for any activity on FunBookr</li>
                    <li>It can be used partially - remaining balance stays in your card</li>
                    <li>Valid for 1 year from date of purchase</li>
                    <li>Non-refundable and non-transferable</li>
                </ul>
            </div>
        </div>
        <div class='footer'>
            <p>This gift card was purchased on {giftCard.PurchasedAt:MMMM dd, yyyy}</p>
            <p>Questions? Contact us at support@funbookr.com</p>
            <p>© 2025 FunBookr. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await _emailService.SendEmailAsync(
                giftCard.RecipientEmail,
                "🎁 You've Received a FunBookr Gift Card!",
                emailHtml);

            _logger.LogInformation("Gift card email sent to: {Email}", giftCard.RecipientEmail);
    }

    /// <summary>
    /// Map entity to DTO
    /// </summary>
    private GiftCardDto MapToDto(GiftCard giftCard)
    {
        return new GiftCardDto
        {
            Id = giftCard.Id,
            Code = giftCard.Code,
            Amount = giftCard.Amount,
            Balance = giftCard.Balance,
            Currency = giftCard.Currency,
            RecipientEmail = giftCard.RecipientEmail,
            RecipientName = giftCard.RecipientName,
            Status = giftCard.Status.ToString(),
            ExpiresAt = giftCard.ExpiresAt,
            PurchasedAt = giftCard.PurchasedAt
        };
    }
}