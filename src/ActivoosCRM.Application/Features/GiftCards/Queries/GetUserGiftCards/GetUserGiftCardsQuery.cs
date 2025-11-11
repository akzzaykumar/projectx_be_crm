using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.GiftCards.Queries.GetUserGiftCards;

/// <summary>
/// Query to get all gift cards for current user
/// </summary>
public class GetUserGiftCardsQuery : IRequest<Result<List<UserGiftCardDto>>>
{
    // Uses current user from ICurrentUserService
}

/// <summary>
/// User gift card DTO
/// </summary>
public class UserGiftCardDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime PurchasedAt { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientName { get; set; }
    public bool IsPurchasedByMe { get; set; }
    public bool IsReceivedByMe { get; set; }
    public int DaysUntilExpiry { get; set; }
    public bool IsExpired { get; set; }
    public bool CanBeUsed { get; set; }
}

/// <summary>
/// Handler for GetUserGiftCardsQuery
/// </summary>
public class GetUserGiftCardsQueryHandler : IRequestHandler<GetUserGiftCardsQuery, Result<List<UserGiftCardDto>>>
{
    private readonly IGiftCardService _giftCardService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetUserGiftCardsQueryHandler> _logger;

    public GetUserGiftCardsQueryHandler(
        IGiftCardService giftCardService,
        ICurrentUserService currentUserService,
        IApplicationDbContext context,
        ILogger<GetUserGiftCardsQueryHandler> logger)
    {
        _giftCardService = giftCardService;
        _currentUserService = currentUserService;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<UserGiftCardDto>>> Handle(
        GetUserGiftCardsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Result<List<UserGiftCardDto>>.CreateFailure("User not authenticated");
            }

            _logger.LogInformation("Getting gift cards for user {UserId}", userId.Value);

            // Get gift cards from service
            var giftCards = await _giftCardService.GetUserGiftCardsAsync(
                userId.Value,
                cancellationToken);

            // Get user email to determine if purchased or received
            var user = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
            var userEmail = user?.Email ?? string.Empty;

            // Map to response DTOs with additional calculated fields
            var response = giftCards.Select(gc =>
            {
                var daysUntilExpiry = gc.ExpiresAt.HasValue
                    ? (int)(gc.ExpiresAt.Value - DateTime.UtcNow).TotalDays
                    : int.MaxValue;

                var isExpired = gc.ExpiresAt.HasValue && gc.ExpiresAt.Value < DateTime.UtcNow;
                var canBeUsed = gc.Balance > 0 && !isExpired && gc.Status == "Active";

                return new UserGiftCardDto
                {
                    Id = gc.Id,
                    Code = gc.Code,
                    Amount = gc.Amount,
                    Balance = gc.Balance,
                    Currency = gc.Currency,
                    Status = gc.Status,
                    ExpiresAt = gc.ExpiresAt,
                    PurchasedAt = gc.PurchasedAt,
                    RecipientEmail = gc.RecipientEmail,
                    RecipientName = gc.RecipientName,
                    IsPurchasedByMe = true, // From service filter
                    IsReceivedByMe = gc.RecipientEmail?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) ?? false,
                    DaysUntilExpiry = Math.Max(0, daysUntilExpiry),
                    IsExpired = isExpired,
                    CanBeUsed = canBeUsed
                };
            }).ToList();

            _logger.LogInformation("Found {Count} gift cards for user", response.Count);

            return Result<List<UserGiftCardDto>>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user gift cards");
            return Result<List<UserGiftCardDto>>.CreateFailure(
                "Failed to retrieve gift cards");
        }
    }
}