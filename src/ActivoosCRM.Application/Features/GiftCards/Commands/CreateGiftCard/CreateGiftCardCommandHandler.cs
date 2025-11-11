using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.GiftCards.Commands.CreateGiftCard;

/// <summary>
/// Handler for CreateGiftCardCommand
/// </summary>
public class CreateGiftCardCommandHandler : IRequestHandler<CreateGiftCardCommand, Result<CreateGiftCardResponse>>
{
    private readonly IGiftCardService _giftCardService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateGiftCardCommandHandler> _logger;

    public CreateGiftCardCommandHandler(
        IGiftCardService giftCardService,
        ICurrentUserService currentUserService,
        ILogger<CreateGiftCardCommandHandler> logger)
    {
        _giftCardService = giftCardService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<CreateGiftCardResponse>> Handle(
        CreateGiftCardCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Result<CreateGiftCardResponse>.CreateFailure("User not authenticated");
            }

            _logger.LogInformation(
                "Creating gift card for user {UserId}, amount: {Amount}",
                userId.Value, request.Amount);

            // Create gift card using service
            var giftCard = await _giftCardService.CreateGiftCardAsync(
                request.Amount,
                request.Currency,
                userId.Value,
                request.RecipientEmail,
                request.RecipientName,
                request.Message,
                cancellationToken);

            var response = new CreateGiftCardResponse
            {
                GiftCardId = giftCard.Id,
                Code = giftCard.Code,
                Amount = giftCard.Amount,
                Currency = giftCard.Currency,
                ExpiresAt = giftCard.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
                RecipientEmail = giftCard.RecipientEmail,
                EmailSent = !string.IsNullOrEmpty(giftCard.RecipientEmail)
            };

            _logger.LogInformation(
                "Gift card created successfully: {Code}",
                response.Code);

            return Result<CreateGiftCardResponse>.CreateSuccess(
                response,
                "Gift card created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gift card");
            return Result<CreateGiftCardResponse>.CreateFailure(
                "Failed to create gift card. Please try again.");
        }
    }
}