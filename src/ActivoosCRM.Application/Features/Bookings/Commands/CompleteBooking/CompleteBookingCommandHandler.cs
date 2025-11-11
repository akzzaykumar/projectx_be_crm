using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Bookings.Commands.CompleteBooking;

public class CompleteBookingCommandHandler : IRequestHandler<CompleteBookingCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly ILogger<CompleteBookingCommandHandler> _logger;

    public CompleteBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILoyaltyService loyaltyService,
        ILogger<CompleteBookingCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _loyaltyService = loyaltyService;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Result<Unit>.CreateFailure("User not authenticated");
        }

        // Get provider profile
        var providerProfile = await _context.ActivityProviders
            .FirstOrDefaultAsync(p => p.UserId == currentUserId, cancellationToken);

        if (providerProfile == null)
        {
            return Result<Unit>.CreateFailure("Activity provider profile not found");
        }

        // Get booking with activity
        var booking = await _context.Bookings
            .Include(b => b.Activity)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            return Result<Unit>.CreateFailure("Booking not found");
        }

        // Verify provider owns the activity
        if (booking.Activity.ProviderId != providerProfile.Id)
        {
            return Result<Unit>.CreateFailure("You do not have permission to complete this booking");
        }

        // Complete the booking
        try
        {
            booking.Complete();
            await _context.SaveChangesAsync(cancellationToken);

            // Award loyalty points for completed booking
            try
            {
                await _loyaltyService.AwardBookingPointsAsync(
                    booking.Id,
                    cancellationToken);

                _logger.LogInformation(
                    "Loyalty points awarded for completed booking {BookingId}",
                    booking.Id);
            }
            catch (Exception loyaltyEx)
            {
                _logger.LogError(loyaltyEx,
                    "Failed to award loyalty points for booking {BookingId}. Booking completion succeeded.",
                    booking.Id);
                // Don't fail booking completion if loyalty points award fails
            }

            return Result<Unit>.CreateSuccess(Unit.Value, "Booking completed successfully");
        }
        catch (InvalidOperationException ex)
        {
            return Result<Unit>.CreateFailure(ex.Message);
        }
    }
}
