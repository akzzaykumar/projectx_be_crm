using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Bookings.Commands.CheckInBooking;

public class CheckInBookingCommandHandler : IRequestHandler<CheckInBookingCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CheckInBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Unit>> Handle(CheckInBookingCommand request, CancellationToken cancellationToken)
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
            return Result<Unit>.CreateFailure("You do not have permission to check-in this booking");
        }

        // Check-in the booking
        try
        {
            booking.CheckIn();
            await _context.SaveChangesAsync(cancellationToken);

            return Result<Unit>.CreateSuccess(Unit.Value, "Booking checked-in successfully");
        }
        catch (InvalidOperationException ex)
        {
            return Result<Unit>.CreateFailure(ex.Message);
        }
    }
}
