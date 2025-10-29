using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Bookings.Commands.ConfirmBooking;

public class ConfirmBookingCommandHandler : IRequestHandler<ConfirmBookingCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ConfirmBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Unit>> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
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
            return Result<Unit>.CreateFailure("You do not have permission to confirm this booking");
        }

        // Confirm the booking
        try
        {
            booking.Confirm(currentUserId.Value);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<Unit>.CreateSuccess(Unit.Value, "Booking confirmed successfully");
        }
        catch (InvalidOperationException ex)
        {
            return Result<Unit>.CreateFailure(ex.Message);
        }
    }
}
