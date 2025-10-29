using ActivoosCRM.Application.Features.Activities.Commands.ArchiveActivity;
using ActivoosCRM.Application.Features.Activities.Commands.CreateActivity;
using ActivoosCRM.Application.Features.Activities.Commands.PublishActivity;
using ActivoosCRM.Application.Features.Activities.Commands.UnpublishActivity;
using ActivoosCRM.Application.Features.Activities.Commands.UpdateActivity;
using ActivoosCRM.Application.Features.Activities.Queries.GetActivities;
using ActivoosCRM.Application.Features.Activities.Queries.GetActivityById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Activities controller - Manages activity listings and bookings
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ActivitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ActivitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all activities with filters and pagination
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/Activities?page=1&amp;pageSize=10&amp;categoryId=550e8400-e29b-41d4-a716-446655440000&amp;minPrice=1000&amp;maxPrice=5000&amp;sortBy=rating&amp;sortOrder=desc
    ///
    /// Query parameters:
    /// - **page**: Page number (default: 1)
    /// - **pageSize**: Items per page (default: 10, max: 50)
    /// - **search**: Search in title/description
    /// - **categoryId**: Filter by category GUID
    /// - **locationId**: Filter by location GUID
    /// - **providerId**: Filter by provider GUID
    /// - **minPrice**: Minimum price filter
    /// - **maxPrice**: Maximum price filter
    /// - **minRating**: Minimum rating filter (0-5)
    /// - **difficultyLevel**: beginner/intermediate/advanced
    /// - **sortBy**: price/rating/popularity/newest (default: newest)
    /// - **sortOrder**: asc/desc (default: desc)
    /// - **featured**: Filter for featured activities only (true/false)
    ///
    /// Returns paginated list of activities with:
    /// - Basic info (title, description, images)
    /// - Pricing (with active discounts)
    /// - Duration and capacity
    /// - Average rating and review count
    /// - Category, location, and provider details
    /// </remarks>
    /// <response code="200">Returns paginated list of activities</response>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActivities([FromQuery] GetActivitiesQuery query)
    {
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get activity details by ID
    /// </summary>
    /// <param name="id">Activity ID</param>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/Activities/550e8400-e29b-41d4-a716-446655440000
    ///
    /// Returns complete activity details including:
    /// - All basic information
    /// - Pricing with discount details
    /// - Requirements (age, skill level, equipment)
    /// - Policies (cancellation, refund, safety)
    /// - Provider information
    /// - Category and location details
    /// - Images gallery
    /// - Available schedules
    /// - Tags
    /// - Statistics (ratings, bookings, views)
    ///
    /// Automatically increments view count when accessed.
    /// </remarks>
    /// <response code="200">Returns activity details</response>
    /// <response code="404">Activity not found</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActivityById(Guid id)
    {
        var result = await _mediator.Send(new GetActivityByIdQuery(id));

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new activity (Provider only)
    /// </summary>
    /// <param name="command">Activity creation details</param>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/Activities
    ///     {
    ///       "categoryId": "550e8400-e29b-41d4-a716-446655440001",
    ///       "locationId": "550e8400-e29b-41d4-a716-446655440002",
    ///       "title": "Scuba Diving Adventure",
    ///       "slug": "scuba-diving-adventure",
    ///       "description": "Comprehensive scuba diving experience for beginners and intermediates. Learn from certified instructors...",
    ///       "shortDescription": "Explore underwater world with expert guidance",
    ///       "price": 3500.00,
    ///       "currency": "INR",
    ///       "maxParticipants": 8,
    ///       "durationMinutes": 180,
    ///       "minAge": 12,
    ///       "maxAge": 65,
    ///       "difficultyLevel": "beginner",
    ///       "whatToBring": "Swimming costume, towel, sunscreen",
    ///       "meetingPoint": "Baga Beach, Goa",
    ///       "cancellationPolicy": "Free cancellation up to 24 hours before"
    ///     }
    ///
    /// Requirements:
    /// - User must be authenticated as a Provider
    /// - Category and Location must exist
    /// - Slug must be unique (URL-friendly: lowercase letters, numbers, hyphens only)
    /// - Description must be at least 50 characters
    /// - Price must be non-negative
    ///
    /// Activity is created in Draft status. Use Publish endpoint to make it available for booking.
    /// </remarks>
    /// <response code="201">Activity created successfully</response>
    /// <response code="400">Validation errors or business rule violations</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized as Provider</response>
    [HttpPost]
    [Authorize(Roles = "ActivityProvider")]
    public async Task<IActionResult> CreateActivity([FromBody] CreateActivityCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(
            nameof(GetActivityById),
            new { id = result.Data },
            new { success = true, message = "Activity created successfully", data = new { activityId = result.Data } });
    }

    /// <summary>
    /// Update an existing activity (Provider only)
    /// </summary>
    /// <param name="id">Activity ID</param>
    /// <param name="command">Activity update details</param>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/Activities/550e8400-e29b-41d4-a716-446655440000
    ///     {
    ///       "activityId": "550e8400-e29b-41d4-a716-446655440000",
    ///       "title": "Scuba Diving Adventure - Updated",
    ///       "description": "Enhanced scuba diving experience with new features...",
    ///       "shortDescription": "Explore underwater world",
    ///       "coverImageUrl": "https://example.com/images/scuba-updated.jpg",
    ///       "price": 3800.00,
    ///       "currency": "INR",
    ///       "minParticipants": 2,
    ///       "maxParticipants": 10,
    ///       "durationMinutes": 200,
    ///       "minAge": 12,
    ///       "difficultyLevel": "beginner",
    ///       "whatToBring": "Swimming costume, towel, sunscreen, water bottle",
    ///       "meetingPoint": "Baga Beach Main Entrance",
    ///       "cancellationPolicy": "Free cancellation up to 48 hours before",
    ///       "refundPolicy": "100% refund for cancellations 48+ hours before",
    ///       "safetyInstructions": "Listen to instructor at all times. Do not touch marine life."
    ///     }
    ///
    /// Requirements:
    /// - Must be the activity owner (Provider who created it)
    /// - Can update pricing, capacity, duration, policies
    /// - Cannot change category, location, or slug
    /// </remarks>
    /// <response code="200">Activity updated successfully</response>
    /// <response code="400">Validation errors</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not the activity owner</response>
    /// <response code="404">Activity not found</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "ActivityProvider")]
    public async Task<IActionResult> UpdateActivity(Guid id, [FromBody] UpdateActivityCommand command)
    {
        if (id != command.ActivityId)
        {
            return BadRequest(new { success = false, message = "Activity ID mismatch" });
        }

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(new { success = true, message = "Activity updated successfully" });
    }

    /// <summary>
    /// Publish an activity to make it available for booking (Provider only)
    /// </summary>
    /// <param name="id">Activity ID</param>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/Activities/550e8400-e29b-41d4-a716-446655440000/publish
    ///
    /// Publishing an activity:
    /// - Changes status from Draft to Active
    /// - Sets IsActive = true
    /// - Sets PublishedAt timestamp
    /// - Makes the activity visible in public listings
    /// - Allows customers to book the activity
    ///
    /// Cannot publish archived activities.
    /// </remarks>
    /// <response code="200">Activity published successfully</response>
    /// <response code="400">Cannot publish (e.g., already archived)</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not the activity owner</response>
    /// <response code="404">Activity not found</response>
    [HttpPut("{id}/publish")]
    [Authorize(Roles = "ActivityProvider")]
    public async Task<IActionResult> PublishActivity(Guid id)
    {
        var result = await _mediator.Send(new PublishActivityCommand(id));

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(new { success = true, message = "Activity published successfully" });
    }

    /// <summary>
    /// Unpublish an activity to temporarily remove it from listings (Provider only)
    /// </summary>
    /// <param name="id">Activity ID</param>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/Activities/550e8400-e29b-41d4-a716-446655440000/unpublish
    ///
    /// Unpublishing an activity:
    /// - Changes status to Inactive
    /// - Sets IsActive = false
    /// - Removes from public listings
    /// - Prevents new bookings
    /// - Can be re-published later
    ///
    /// Use this for seasonal activities or temporary unavailability.
    /// </remarks>
    /// <response code="200">Activity unpublished successfully</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not the activity owner</response>
    /// <response code="404">Activity not found</response>
    [HttpPut("{id}/unpublish")]
    [Authorize(Roles = "ActivityProvider")]
    public async Task<IActionResult> UnpublishActivity(Guid id)
    {
        var result = await _mediator.Send(new UnpublishActivityCommand(id));

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(new { success = true, message = "Activity unpublished successfully" });
    }

    /// <summary>
    /// Archive an activity permanently (Provider only)
    /// </summary>
    /// <param name="id">Activity ID</param>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /api/Activities/550e8400-e29b-41d4-a716-446655440000
    ///
    /// Archiving an activity:
    /// - Changes status to Archived
    /// - Sets IsActive = false
    /// - Removes from all listings
    /// - Cannot be re-published
    /// - Historical bookings and data preserved
    ///
    /// Use this for activities that are permanently discontinued.
    /// Archived activities cannot be restored.
    /// </remarks>
    /// <response code="200">Activity archived successfully</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not the activity owner</response>
    /// <response code="404">Activity not found</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ActivityProvider")]
    public async Task<IActionResult> ArchiveActivity(Guid id)
    {
        var result = await _mediator.Send(new ArchiveActivityCommand(id));

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(new { success = true, message = "Activity archived successfully" });
    }
}
