using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Loyalty.Commands.RedeemPoints;

/// <summary>
/// Command to redeem loyalty points for booking discount
/// </summary>
public class RedeemPointsCommand : IRequest<Result<RedeemPointsResponse>>
{
    public Guid BookingId { get; set; }
    public int Points { get; set; }
}

/// <summary>
/// Response for redeem points command
/// </summary>
public class RedeemPointsResponse
{
    public Guid BookingId { get; set; }
    public int PointsRedeemed { get; set; }
    public decimal DiscountAmount { get; set; }
    public int RemainingPoints { get; set; }
    public decimal UpdatedBookingTotal { get; set; }
}

/// <summary>
/// Validator for RedeemPointsCommand
/// </summary>
public class RedeemPointsCommandValidator : AbstractValidator<RedeemPointsCommand>
{
    public RedeemPointsCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("Booking ID is required");

        RuleFor(x => x.Points)
            .GreaterThanOrEqualTo(100)
            .WithMessage("Minimum 100 points required for redemption")
            .LessThanOrEqualTo(100000)
            .WithMessage("Maximum 100,000 points can be redeemed at once");
    }
}

/// <summary>
/// Handler for RedeemPointsCommand
/// </summary>
public class RedeemPointsCommandHandler : IRequestHandler<RedeemPointsCommand, Result<RedeemPointsResponse>>
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RedeemPointsCommandHandler> _logger;

    public RedeemPointsCommandHandler(
        ILoyaltyService loyaltyService,
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<RedeemPointsCommandHandler> logger)
    {
        _loyaltyService = loyaltyService;
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<RedeemPointsResponse>> Handle(
        RedeemPointsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Result<RedeemPointsResponse>.CreateFailure("User not authenticated");
            }

            _logger.LogInformation(
                "Redeeming {Points} points for user {UserId} on booking {BookingId}",
                request.Points, userId.Value, request.BookingId);

            // Verify booking ownership
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null)
            {
                return Result<RedeemPointsResponse>.CreateFailure("Booking not found");
            }

            if (booking.Customer.UserId != userId.Value)
            {
                return Result<RedeemPointsResponse>.CreateFailure(
                    "You can only redeem points on your own bookings");
            }

            if (booking.IsPaid)
            {
                return Result<RedeemPointsResponse>.CreateFailure(
                    "Cannot redeem points on already paid booking");
            }

            // Redeem points via service
            var discountAmount = await _loyaltyService.RedeemPointsAsync(
                userId.Value,
                request.Points,
                request.BookingId,
                cancellationToken);

            // Get updated loyalty status
            var updatedStatus = await _loyaltyService.GetUserLoyaltyStatusAsync(
                userId.Value,
                cancellationToken);

            var response = new RedeemPointsResponse
            {
                BookingId = request.BookingId,
                PointsRedeemed = request.Points,
                DiscountAmount = discountAmount,
                RemainingPoints = updatedStatus.AvailablePoints,
                UpdatedBookingTotal = booking.TotalAmount - discountAmount
            };

            _logger.LogInformation(
                "Points redeemed successfully. Discount: ₹{Discount}, Remaining: {Points} points",
                discountAmount, updatedStatus.AvailablePoints);

            return Result<RedeemPointsResponse>.CreateSuccess(
                response,
                $"Successfully redeemed {request.Points} points for ₹{discountAmount:N2} discount");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to redeem points");
            return Result<RedeemPointsResponse>.CreateFailure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redeeming loyalty points");
            return Result<RedeemPointsResponse>.CreateFailure(
                "Failed to redeem points. Please try again.");
        }
    }
}