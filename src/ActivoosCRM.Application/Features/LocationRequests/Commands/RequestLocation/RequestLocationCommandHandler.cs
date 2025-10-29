using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.LocationRequests.Commands.RequestLocation;

public class RequestLocationCommandHandler : IRequestHandler<RequestLocationCommand, Result<RequestLocationResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RequestLocationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<RequestLocationResponse>> Handle(RequestLocationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Result<RequestLocationResponse>.CreateFailure("User not authenticated");
        }

        // Get provider profile
        var providerProfile = await _context.ActivityProviders
            .FirstOrDefaultAsync(p => p.UserId == currentUserId, cancellationToken);

        if (providerProfile == null)
        {
            return Result<RequestLocationResponse>.CreateFailure("Activity provider profile not found");
        }

        // Check for duplicate pending requests for same location
        var duplicateExists = await _context.LocationRequests
            .AnyAsync(lr =>
                lr.ProviderId == providerProfile.Id &&
                lr.Name.ToLower() == request.Name.ToLower() &&
                lr.City.ToLower() == request.City.ToLower() &&
                lr.State.ToLower() == request.State.ToLower() &&
                lr.Status == Domain.Enums.LocationRequestStatus.Pending,
                cancellationToken);

        if (duplicateExists)
        {
            return Result<RequestLocationResponse>.CreateFailure(
                "You already have a pending request for this location. Please wait for admin review.");
        }

        // Check if location already exists
        var locationExists = await _context.Locations
            .AnyAsync(l =>
                l.Name.ToLower() == request.Name.ToLower() &&
                l.City.ToLower() == request.City.ToLower() &&
                l.State.ToLower() == request.State.ToLower(),
                cancellationToken);

        if (locationExists)
        {
            return Result<RequestLocationResponse>.CreateFailure(
                "This location already exists in the system. You can use it directly when creating activities.");
        }

        // Create location request
        var locationRequest = LocationRequest.Create(
            providerProfile.Id,
            request.Name,
            request.City,
            request.State,
            request.Country,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.Reason);

        _context.LocationRequests.Add(locationRequest);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new RequestLocationResponse
        {
            LocationRequestId = locationRequest.Id,
            Status = locationRequest.Status.ToString(),
            Message = "Location request submitted successfully. An admin will review it shortly."
        };

        return Result<RequestLocationResponse>.CreateSuccess(response, "Location request created successfully");
    }
}
