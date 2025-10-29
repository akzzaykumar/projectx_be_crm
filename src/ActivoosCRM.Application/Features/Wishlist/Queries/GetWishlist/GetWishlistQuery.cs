using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Wishlist.Queries.GetWishlist;

/// <summary>
/// Query to get user's wishlist
/// </summary>
public record GetWishlistQuery() : IRequest<Result<List<WishlistItemDto>>>;

/// <summary>
/// DTO containing wishlist item details with full activity information
/// </summary>
public class WishlistItemDto
{
    public Guid WishlistId { get; set; }
    public WishlistActivityDto Activity { get; set; } = null!;
    public DateTime AddedAt { get; set; }
}

/// <summary>
/// Activity information within wishlist item
/// </summary>
public class WishlistActivityDto
{
    public Guid ActivityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public decimal AverageRating { get; set; }
    public WishlistLocationDto Location { get; set; } = null!;
    public WishlistProviderDto Provider { get; set; } = null!;
}

/// <summary>
/// Location information within wishlist activity
/// </summary>
public class WishlistLocationDto
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Provider information within wishlist activity
/// </summary>
public class WishlistProviderDto
{
    public string BusinessName { get; set; } = string.Empty;
}
