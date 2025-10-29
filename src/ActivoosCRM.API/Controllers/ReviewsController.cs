using ActivoosCRM.Application.Features.Reviews.Commands.CreateReview;
using ActivoosCRM.Application.Features.Reviews.Commands.MarkReviewHelpful;
using ActivoosCRM.Application.Features.Reviews.Queries.GetReviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Review management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IMediator mediator, ILogger<ReviewsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get reviews with filters and pagination
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/Reviews?activityId=550e8400-e29b-41d4-a716-446655440000&amp;rating=5&amp;page=1&amp;pageSize=10
    /// 
    /// Query Parameters:
    /// - activityId (guid, optional): Filter by activity
    /// - providerId (guid, optional): Filter by provider
    /// - customerId (guid, optional): Filter by customer
    /// - rating (int, optional): Filter by rating (1-5)
    /// - isVerified (bool, optional): Filter verified reviews
    /// - page (int, default=1): Page number
    /// - pageSize (int, default=10, max=50): Items per page
    /// 
    /// Response includes:
    /// - Paginated list of reviews with customer and activity details
    /// - Average rating across all filtered reviews
    /// - Rating distribution (count of 1-5 star reviews)
    /// - Total count, page number, total pages
    /// </remarks>
    /// <param name="activityId">Filter by activity ID</param>
    /// <param name="providerId">Filter by provider ID</param>
    /// <param name="customerId">Filter by customer ID</param>
    /// <param name="rating">Filter by rating (1-5)</param>
    /// <param name="isVerified">Filter verified reviews</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 50)</param>
    /// <response code="200">Reviews retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetReviewsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReviews(
        [FromQuery] Guid? activityId = null,
        [FromQuery] Guid? providerId = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] int? rating = null,
        [FromQuery] bool? isVerified = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetReviewsQuery
        {
            ActivityId = activityId,
            ProviderId = providerId,
            CustomerId = customerId,
            Rating = rating,
            IsVerified = isVerified,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Create a review for a completed booking
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/Reviews
    ///     {
    ///         "bookingId": "550e8400-e29b-41d4-a716-446655440000",
    ///         "rating": 5,
    ///         "title": "Amazing Experience!",
    ///         "reviewText": "Had an incredible time scuba diving. The instructor was very professional and made us feel safe throughout the experience."
    ///     }
    /// 
    /// Business Rules:
    /// - Customer must be authenticated
    /// - Booking must belong to the current user
    /// - Booking must be completed
    /// - Customer can only review each booking once
    /// - Rating must be between 1 and 5
    /// - Title is optional (max 200 characters)
    /// - Review text is optional (max 2000 characters)
    /// 
    /// After review creation:
    /// - Activity average rating is recalculated
    /// - Provider average rating is recalculated
    /// - Review counts are updated
    /// </remarks>
    /// <param name="command">Review creation details</param>
    /// <response code="201">Review created successfully</response>
    /// <response code="400">Invalid request or business rule violation</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User not authorized to review this booking</response>
    /// <response code="404">Booking not found</response>
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(CreateReviewResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(
            nameof(GetReviews),
            new { activityId = (Guid?)null },
            result);
    }

    /// <summary>
    /// Mark a review as helpful
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/Reviews/550e8400-e29b-41d4-a716-446655440000/helpful
    /// 
    /// Increments the helpful count for the review.
    /// Users must be authenticated to mark reviews as helpful.
    /// 
    /// Note: In a production system, this would track which users
    /// have marked each review as helpful to prevent duplicates.
    /// </remarks>
    /// <param name="id">Review ID</param>
    /// <response code="200">Review marked as helpful</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Review not found</response>
    [HttpPut("{id}/helpful")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkReviewHelpful(Guid id)
    {
        var command = new MarkReviewHelpfulCommand { ReviewId = id };
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
