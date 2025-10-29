using ActivoosCRM.Application.Features.Wishlist.Commands.AddToWishlist;
using ActivoosCRM.Application.Features.Wishlist.Commands.RemoveFromWishlist;
using ActivoosCRM.Application.Features.Wishlist.Queries.GetWishlist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Controller for managing user's wishlist of favorite activities
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(IMediator mediator, ILogger<WishlistController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get user's wishlist with full activity details
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/wishlist
    /// 
    /// **Returns:**
    /// - List of wishlist items ordered by date added (most recent first)
    /// - Each item includes complete activity information:
    ///   - Basic details: title, cover image, price (original and discounted)
    ///   - Ratings: average rating to help decision making
    ///   - Location: where the activity takes place
    ///   - Provider: business offering the activity
    ///   - Timestamp: when added to wishlist
    /// 
    /// **Business Rules:**
    /// - Only returns authenticated user's own wishlist items
    /// - Shows current pricing (may have changed since adding to wishlist)
    /// - Includes discounted price if activity has active discount
    /// - Ordered by most recently added first
    /// 
    /// **Use Cases:**
    /// - Display "My Wishlist" or "Favorites" page
    /// - Show saved activities for later booking
    /// - Compare prices of favorite activities
    /// - Quick access to user's preferred activities
    /// - Share wishlist with friends/family
    /// 
    /// **Response Structure:**
    /// - Array of wishlist items (empty if no items)
    /// - Each item has wishlistId for removal operations
    /// - Activity details are current (live data, not cached)
    /// - Pricing reflects any active discounts or changes
    /// </remarks>
    /// <returns>List of user's wishlist items with full activity details</returns>
    /// <response code="200">Returns user's wishlist items</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<WishlistItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetWishlist()
    {
        var query = new GetWishlistQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Add activity to user's wishlist
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/wishlist
    ///     {
    ///       "activityId": "550e8400-e29b-41d4-a716-446655440001"
    ///     }
    /// 
    /// **Validation:**
    /// - Activity must exist in the system
    /// - User must be authenticated
    /// - Activity cannot already be in user's wishlist (duplicate check)
    /// 
    /// **Business Rules:**
    /// - Each activity can only be added once per user
    /// - No limit on total number of wishlist items
    /// - Timestamp recorded for when item was added
    /// - Activity must be active (system validates)
    /// 
    /// **Use Cases:**
    /// - User clicks "Add to Wishlist" or "Save" button on activity page
    /// - Heart/favorite icon toggle on activity cards
    /// - Save interesting activities for later consideration
    /// - Build collection of activities for trip planning
    /// - Share favorite activities with travel companions
    /// 
    /// **Error Cases:**
    /// - Activity not found: Returns 400 with "Activity not found" message
    /// - Already in wishlist: Returns 400 with "Activity is already in your wishlist"
    /// - Invalid activity ID: Returns 400 with validation error
    /// </remarks>
    /// <param name="command">Command containing activity ID to add</param>
    /// <returns>Success message if activity added to wishlist</returns>
    /// <response code="200">Activity successfully added to wishlist</response>
    /// <response code="400">Activity not found or already in wishlist</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> AddToWishlist([FromBody] AddToWishlistCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Remove activity from user's wishlist
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     DELETE /api/wishlist/550e8400-e29b-41d4-a716-446655440001
    /// 
    /// **Route Parameter:**
    /// - `activityId` (guid, required): The ID of the activity to remove from wishlist
    /// 
    /// **Validation:**
    /// - Activity must be in user's wishlist
    /// - User must own the wishlist item (automatic via authentication)
    /// - Activity ID must be valid GUID format
    /// 
    /// **Business Rules:**
    /// - Only removes from authenticated user's wishlist (ownership enforced)
    /// - Cannot remove items from other users' wishlists
    /// - Permanent deletion (not soft delete)
    /// - No confirmation required (frontend should handle confirmation)
    /// 
    /// **Use Cases:**
    /// - User clicks "Remove from Wishlist" or unfavorites activity
    /// - Heart/favorite icon toggle (removing favorite)
    /// - Cleanup of old/unwanted saved activities
    /// - After booking activity (automatically remove from wishlist)
    /// - Managing wishlist space/organization
    /// 
    /// **Error Cases:**
    /// - Not in wishlist: Returns 400 with "Activity not found in your wishlist"
    /// - Invalid activity ID: Returns 400 with validation error
    /// - Not authenticated: Returns 401 Unauthorized
    /// 
    /// **Success Response:**
    /// - Returns 200 OK with success message
    /// - Item immediately removed from database
    /// - Subsequent GET will not include this activity
    /// </remarks>
    /// <param name="activityId">The ID of the activity to remove from wishlist</param>
    /// <returns>Success message if activity removed from wishlist</returns>
    /// <response code="200">Activity successfully removed from wishlist</response>
    /// <response code="400">Activity not found in wishlist or validation error</response>
    /// <response code="401">User not authenticated</response>
    [HttpDelete("{activityId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> RemoveFromWishlist([FromRoute] Guid activityId)
    {
        var command = new RemoveFromWishlistCommand(activityId);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
