using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Wishlist.Queries.GetWishlist;

public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, Result<List<WishlistItemDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetWishlistQueryHandler> _logger;

    public GetWishlistQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetWishlistQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<List<WishlistItemDto>>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
            {
                return Result<List<WishlistItemDto>>.CreateFailure("User not authenticated");
            }

            // Get user's wishlist with activity details
            var wishlistItems = await _context.Wishlists
                .Where(w => w.CustomerId == currentUserId.Value)
                .Include(w => w.Activity)
                    .ThenInclude(a => a.Location)
                .Include(w => w.Activity)
                    .ThenInclude(a => a.Provider)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new WishlistItemDto
                {
                    WishlistId = w.Id,
                    Activity = new WishlistActivityDto
                    {
                        ActivityId = w.Activity.Id,
                        Title = w.Activity.Title,
                        CoverImageUrl = w.Activity.CoverImageUrl,
                        Price = w.Activity.Price,
                        DiscountedPrice = w.Activity.DiscountedPrice,
                        AverageRating = w.Activity.AverageRating,
                        Location = new WishlistLocationDto
                        {
                            Name = w.Activity.Location.Name
                        },
                        Provider = new WishlistProviderDto
                        {
                            BusinessName = w.Activity.Provider.BusinessName
                        }
                    },
                    AddedAt = w.CreatedAt
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {WishlistCount} wishlist items for user {UserId}",
                wishlistItems.Count, currentUserId.Value);

            return Result<List<WishlistItemDto>>.CreateSuccess(wishlistItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wishlist for current user");
            return Result<List<WishlistItemDto>>.CreateFailure("Failed to retrieve wishlist");
        }
    }
}
