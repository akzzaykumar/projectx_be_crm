using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.GiftCards.Queries.ValidateGiftCard;

/// <summary>
/// Query to validate a gift card code
/// </summary>
public class ValidateGiftCardQuery : IRequest<Result<ValidateGiftCardResponse>>
{
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Response for validate gift card query
/// </summary>
public class ValidateGiftCardResponse
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime? ExpiresAt { get; set; }
    public int DaysUntilExpiry { get; set; }
    public bool IsExpired { get; set; }
}

/// <summary>
/// Validator for ValidateGiftCardQuery
/// </summary>
public class ValidateGiftCardQueryValidator : AbstractValidator<ValidateGiftCardQuery>
{
    public ValidateGiftCardQueryValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Gift card code is required")
            .Matches(@"^FB-\d{4}-\d{4}-\d{4}$")
            .WithMessage("Invalid gift card code format. Expected format: FB-XXXX-XXXX-XXXX");
    }
}

/// <summary>
/// Handler for ValidateGiftCardQuery
/// </summary>
public class ValidateGiftCardQueryHandler : IRequestHandler<ValidateGiftCardQuery, Result<ValidateGiftCardResponse>>
{
    private readonly IGiftCardService _giftCardService;
    private readonly ILogger<ValidateGiftCardQueryHandler> _logger;

    public ValidateGiftCardQueryHandler(
        IGiftCardService giftCardService,
        ILogger<ValidateGiftCardQueryHandler> logger)
    {
        _giftCardService = giftCardService;
        _logger = logger;
    }

    public async Task<Result<ValidateGiftCardResponse>> Handle(
        ValidateGiftCardQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Validating gift card: {Code}", request.Code);

            var validationResult = await _giftCardService.ValidateGiftCardAsync(
                request.Code,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                return Result<ValidateGiftCardResponse>.CreateSuccess(
                    new ValidateGiftCardResponse
                    {
                        IsValid = false,
                        ErrorMessage = validationResult.ErrorMessage
                    });
            }

            // Get full balance details
            var balanceDto = await _giftCardService.GetGiftCardBalanceAsync(
                request.Code,
                cancellationToken);

            var response = new ValidateGiftCardResponse
            {
                IsValid = true,
                Balance = balanceDto.Balance,
                Currency = balanceDto.Currency,
                ExpiresAt = balanceDto.ExpiresAt,
                DaysUntilExpiry = balanceDto.DaysUntilExpiry,
                IsExpired = balanceDto.IsExpired
            };

            return Result<ValidateGiftCardResponse>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating gift card: {Code}", request.Code);
            return Result<ValidateGiftCardResponse>.CreateFailure(
                "An error occurred while validating the gift card");
        }
    }
}