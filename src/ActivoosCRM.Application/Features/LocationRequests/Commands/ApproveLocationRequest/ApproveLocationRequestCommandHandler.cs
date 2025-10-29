using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using ActivoosCRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.LocationRequests.Commands.ApproveLocationRequest;

public class ApproveLocationRequestCommandHandler : IRequestHandler<ApproveLocationRequestCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ApproveLocationRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Unit>> Handle(ApproveLocationRequestCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Result<Unit>.CreateFailure("User not authenticated");
        }

        // Get location request
        var locationRequest = await _context.LocationRequests
            .FirstOrDefaultAsync(lr => lr.Id == request.LocationRequestId, cancellationToken);

        if (locationRequest == null)
        {
            return Result<Unit>.CreateFailure("Location request not found");
        }

        if (locationRequest.Status != LocationRequestStatus.Pending)
        {
            return Result<Unit>.CreateFailure($"Cannot approve location request in {locationRequest.Status} status");
        }

        // Check if location already exists (in case it was created after the request)
        var existingLocation = await _context.Locations
            .FirstOrDefaultAsync(l =>
                l.Name.ToLower() == locationRequest.Name.ToLower() &&
                l.City.ToLower() == locationRequest.City.ToLower() &&
                l.State.ToLower() == locationRequest.State.ToLower(),
                cancellationToken);

        Location location;

        if (existingLocation != null)
        {
            // Link to existing location
            location = existingLocation;
        }
        else
        {
            // Create new location
            location = Location.Create(
                locationRequest.Name,
                locationRequest.City,
                locationRequest.State,
                locationRequest.Country,
                postalCode: null,
                addressLine1: locationRequest.Address,
                addressLine2: null,
                latitude: locationRequest.Latitude,
                longitude: locationRequest.Longitude,
                description: null);

            _context.Locations.Add(location);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Approve the request
        try
        {
            locationRequest.Approve(currentUserId.Value, location.Id);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<Unit>.CreateSuccess(Unit.Value, "Location request approved successfully");
        }
        catch (InvalidOperationException ex)
        {
            return Result<Unit>.CreateFailure(ex.Message);
        }
    }
}
