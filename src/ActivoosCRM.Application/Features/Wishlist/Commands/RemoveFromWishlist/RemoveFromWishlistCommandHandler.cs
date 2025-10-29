using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Wishlist.Commands.RemoveFromWishlist;

public class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RemoveFromWishlistCommandHandler> _logger;

    public RemoveFromWishlistCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<RemoveFromWishlistCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return Result<bool>.CreateFailure("User not authenticated");
            }

            // Find wishlist entry (ownership validation)
            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(
                    w => w.CustomerId == currentUserId.Value && w.ActivityId == request.ActivityId,
                    cancellationToken);

            if (wishlistItem == null)
            {
                return Result<bool>.CreateFailure("Activity not found in your wishlist");
            }

            // Remove from wishlist
            _context.Wishlists.Remove(wishlistItem);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Activity {ActivityId} removed from wishlist for user {UserId}",
                request.ActivityId, currentUserId.Value);

            return Result<bool>.CreateSuccess(true, "Activity removed from wishlist");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing activity {ActivityId} from wishlist", request.ActivityId);
            return Result<bool>.CreateFailure("Failed to remove activity from wishlist");
        }
    }
}
