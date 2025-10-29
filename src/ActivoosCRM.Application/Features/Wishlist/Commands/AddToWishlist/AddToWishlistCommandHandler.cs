using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Wishlist.Commands.AddToWishlist;

public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AddToWishlistCommandHandler> _logger;

    public AddToWishlistCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<AddToWishlistCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return Result<bool>.CreateFailure("User not authenticated");
            }

            // Check if activity exists
            var activityExists = await _context.Activities
                .AnyAsync(a => a.Id == request.ActivityId, cancellationToken);

            if (!activityExists)
            {
                return Result<bool>.CreateFailure("Activity not found");
            }

            // Check if already in wishlist (prevent duplicates)
            var existingWishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(
                    w => w.CustomerId == currentUserId.Value && w.ActivityId == request.ActivityId,
                    cancellationToken);

            if (existingWishlistItem != null)
            {
                return Result<bool>.CreateFailure("Activity is already in your wishlist");
            }

            // Create wishlist entry
            var wishlist = Domain.Entities.Wishlist.Create(currentUserId.Value, request.ActivityId);
            _context.Wishlists.Add(wishlist);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Activity {ActivityId} added to wishlist for user {UserId}",
                request.ActivityId, currentUserId.Value);

            return Result<bool>.CreateSuccess(true, "Activity added to wishlist");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding activity {ActivityId} to wishlist", request.ActivityId);
            return Result<bool>.CreateFailure("Failed to add activity to wishlist");
        }
    }
}
