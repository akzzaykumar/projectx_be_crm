using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.GiftCards.Commands.ApplyGiftCard;

/// <summary>
/// Command to apply a gift card to a booking
/// </summary>
public class ApplyGiftCardCommand : IRequest<Result<ApplyGiftCardResponse>>
{
    public Guid BookingId { get; set; }
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Response for apply gift card command
/// </summary>
public class ApplyGiftCardResponse
{
    public Guid BookingId { get; set; }
    public string GiftCardCode { get; set; } = string.Empty;
    public decimal AmountApplied { get; set; }
    public decimal RemainingBalance { get; set; }
    public decimal UpdatedBookingTotal { get; set; }
}

/// <summary>
/// Validator for ApplyGiftCardCommand
/// </summary>
public class ApplyGiftCardCommandValidator : AbstractValidator<ApplyGiftCardCommand>
{
    public ApplyGiftCardCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("Booking ID is required");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Gift card code is required")
            .Matches(@"^FB-\d{4}-\d{4}-\d{4}$")
            .WithMessage("Invalid gift card code format");
    }
}

/// <summary>
/// Handler for ApplyGiftCardCommand
/// </summary>
public class ApplyGiftCardCommandHandler : IRequestHandler<ApplyGiftCardCommand, Result<ApplyGiftCardResponse>>
{
    private readonly IGiftCardService _giftCardService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ApplyGiftCardCommandHandler> _logger;

    public ApplyGiftCardCommandHandler(
        IGiftCardService giftCardService,
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<ApplyGiftCardCommandHandler> logger)
    {
        _giftCardService = giftCardService;
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<ApplyGiftCardResponse>> Handle(
        ApplyGiftCardCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Result<ApplyGiftCardResponse>.CreateFailure("User not authenticated");
            }

            _logger.LogInformation(
                "Applying gift card {Code} to booking {BookingId}",
                request.Code, request.BookingId);

            // Apply gift card via service
            var amountApplied = await _giftCardService.ApplyGiftCardToBookingAsync(
                request.BookingId,
                request.Code,
                userId.Value,
                cancellationToken);

            // Get updated gift card balance
            var balance = await _giftCardService.GetGiftCardBalanceAsync(
                request.Code,
                cancellationToken);

            // Get updated booking
            var booking = await _context.Bookings
                .FindAsync(new object[] { request.BookingId }, cancellationToken);

            if (booking == null)
            {
                return Result<ApplyGiftCardResponse>.CreateFailure("Booking not found");
            }

            var response = new ApplyGiftCardResponse
            {
                BookingId = request.BookingId,
                GiftCardCode = request.Code,
                AmountApplied = amountApplied,
                RemainingBalance = balance.Balance,
                UpdatedBookingTotal = booking.TotalAmount - amountApplied
            };

            _logger.LogInformation(
                "Gift card applied successfully. Amount: {Amount}, Remaining: {Remaining}",
                amountApplied, balance.Balance);

            return Result<ApplyGiftCardResponse>.CreateSuccess(
                response,
                $"Gift card applied successfully. ₹{amountApplied:N2} discount applied.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to apply gift card: {Code}", request.Code);
            return Result<ApplyGiftCardResponse>.CreateFailure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying gift card");
            return Result<ApplyGiftCardResponse>.CreateFailure(
                "Failed to apply gift card. Please try again.");
        }
    }
}