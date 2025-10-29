using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.LocationRequests.Commands.RejectLocationRequest;

public class RejectLocationRequestCommandHandler : IRequestHandler<RejectLocationRequestCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RejectLocationRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Unit>> Handle(RejectLocationRequestCommand request, CancellationToken cancellationToken)
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
            return Result<Unit>.CreateFailure($"Cannot reject location request in {locationRequest.Status} status");
        }

        // Reject the request
        try
        {
            locationRequest.Reject(currentUserId.Value, request.RejectionReason);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<Unit>.CreateSuccess(Unit.Value, "Location request rejected successfully");
        }
        catch (InvalidOperationException ex)
        {
            return Result<Unit>.CreateFailure(ex.Message);
        }
    }
}
