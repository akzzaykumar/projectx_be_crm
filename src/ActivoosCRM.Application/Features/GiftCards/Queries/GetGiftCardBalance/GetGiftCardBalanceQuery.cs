using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.GiftCards.Queries.GetGiftCardBalance;

/// <summary>
/// Query to get gift card balance
/// </summary>
public class GetGiftCardBalanceQuery : IRequest<Result<GiftCardBalanceResponse>>
{
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Response for get gift card balance query
/// </summary>
public class GiftCardBalanceResponse
{
    public string Code { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal OriginalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime? ExpiresAt { get; set; }
    public int DaysUntilExpiry { get; set; }
    public bool IsExpired { get; set; }
    public bool CanBeUsed { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Validator for GetGiftCardBalanceQuery
/// </summary>
public class GetGiftCardBalanceQueryValidator : AbstractValidator<GetGiftCardBalanceQuery>
{
    public GetGiftCardBalanceQueryValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Gift card code is required")
            .Matches(@"^FB-\d{4}-\d{4}-\d{4}$")
            .WithMessage("Invalid gift card code format");
    }
}

/// <summary>
/// Handler for GetGiftCardBalanceQuery
/// </summary>
public class GetGiftCardBalanceQueryHandler : IRequestHandler<GetGiftCardBalanceQuery, Result<GiftCardBalanceResponse>>
{
    private readonly IGiftCardService _giftCardService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetGiftCardBalanceQueryHandler> _logger;

    public GetGiftCardBalanceQueryHandler(
        IGiftCardService giftCardService,
        IApplicationDbContext context,
        ILogger<GetGiftCardBalanceQueryHandler> logger)
    {
        _giftCardService = giftCardService;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<GiftCardBalanceResponse>> Handle(
        GetGiftCardBalanceQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting balance for gift card: {Code}", request.Code);

            var balanceDto = await _giftCardService.GetGiftCardBalanceAsync(
                request.Code,
                cancellationToken);

            // Get original amount from gift card
            var giftCard = await _context.GiftCards.FirstOrDefaultAsync(gc => gc.Code == request.Code.ToUpperInvariant(), cancellationToken);

            if (giftCard == null)
            {
                return Result<GiftCardBalanceResponse>.CreateFailure("Gift card not found");
            }

            var response = new GiftCardBalanceResponse
            {
                Code = balanceDto.Code,
                Balance = balanceDto.Balance,
                OriginalAmount = giftCard.Amount,
                Currency = balanceDto.Currency,
                ExpiresAt = balanceDto.ExpiresAt,
                DaysUntilExpiry = balanceDto.DaysUntilExpiry,
                IsExpired = balanceDto.IsExpired,
                CanBeUsed = balanceDto.Balance > 0 && !balanceDto.IsExpired,
                Status = giftCard.Status.ToString()
            };

            return Result<GiftCardBalanceResponse>.CreateSuccess(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Gift card not found: {Code}", request.Code);
            return Result<GiftCardBalanceResponse>.CreateFailure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gift card balance");
            return Result<GiftCardBalanceResponse>.CreateFailure(
                "Failed to retrieve gift card balance");
        }
    }
}