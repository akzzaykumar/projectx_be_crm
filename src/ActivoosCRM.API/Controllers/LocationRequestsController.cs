using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Application.Features.LocationRequests.Commands.ApproveLocationRequest;
using ActivoosCRM.Application.Features.LocationRequests.Commands.RejectLocationRequest;
using ActivoosCRM.Application.Features.LocationRequests.Commands.RequestLocation;
using ActivoosCRM.Application.Features.LocationRequests.Queries.GetLocationRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// API controller for location request management
/// Handles provider location requests and admin approval/rejection workflow
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LocationRequestsController> _logger;

    public LocationRequestsController(IMediator mediator, ILogger<LocationRequestsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new location request (Activity Providers only)
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/LocationRequests
    ///     {
    ///         "name": "Marine Drive",
    ///         "city": "Mumbai",
    ///         "state": "Maharashtra",
    ///         "country": "India",
    ///         "address": "Marine Drive, Mumbai, Maharashtra 400020",
    ///         "latitude": 18.9432,
    ///         "longitude": 72.8236,
    ///         "reason": "We offer water sports activities at Marine Drive and need this location added to provide our services"
    ///     }
    /// 
    /// Response:
    /// 
    ///     {
    ///         "isSuccess": true,
    ///         "value": {
    ///             "locationRequestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///             "status": "Pending",
    ///             "message": "Location request submitted successfully. An admin will review it shortly."
    ///         }
    ///     }
    /// 
    /// </remarks>
    /// <param name="command">Location request details</param>
    /// <returns>Created location request ID and status</returns>
    /// <response code="201">Location request created successfully</response>
    /// <response code="400">Invalid request data or duplicate request exists</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not an Activity Provider</response>
    [HttpPost]
    [Authorize(Roles = "ActivityProvider")]
    [ProducesResponseType(typeof(Result<RequestLocationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RequestLocation([FromBody] RequestLocationCommand command)
    {
        _logger.LogInformation("Processing location request for {Name}, {City}", command.Name, command.City);

        var result = await _mediator.Send(command);

        if (result.Success)
        {
            return CreatedAtAction(
                nameof(GetLocationRequests),
                new { },
                result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Get all location requests with optional filters (Admin only)
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/LocationRequests?Status=Pending&amp;Page=1&amp;PageSize=20
    /// 
    /// Response:
    /// 
    ///     {
    ///         "isSuccess": true,
    ///         "value": {
    ///             "items": [
    ///                 {
    ///                     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///                     "name": "Marine Drive",
    ///                     "city": "Mumbai",
    ///                     "state": "Maharashtra",
    ///                     "country": "India",
    ///                     "address": "Marine Drive, Mumbai, Maharashtra 400020",
    ///                     "latitude": 18.9432,
    ///                     "longitude": 72.8236,
    ///                     "reason": "We offer water sports activities at Marine Drive...",
    ///                     "status": "Pending",
    ///                     "createdAt": "2025-01-26T10:00:00Z",
    ///                     "reviewedAt": null,
    ///                     "rejectionReason": null,
    ///                     "locationId": null,
    ///                     "provider": {
    ///                         "id": "guid",
    ///                         "businessName": "Adventure Sports Co",
    ///                         "contactEmail": "contact@adventuresports.com"
    ///                     },
    ///                     "createdLocation": null
    ///                 }
    ///             ],
    ///             "pageNumber": 1,
    ///             "totalPages": 1,
    ///             "totalCount": 1,
    ///             "hasPreviousPage": false,
    ///             "hasNextPage": false
    ///         }
    ///     }
    /// 
    /// </remarks>
    /// <param name="query">Query parameters including filters and pagination</param>
    /// <returns>Paginated list of location requests</returns>
    /// <response code="200">Location requests retrieved successfully</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not an Admin</response>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<PaginatedList<LocationRequestDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLocationRequests([FromQuery] GetLocationRequestsQuery query)
    {
        _logger.LogInformation("Retrieving location requests - Status: {Status}, ProviderId: {ProviderId}",
            query.Status, query.ProviderId);

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Approve a location request and create the location (Admin only)
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/LocationRequests/3fa85f64-5717-4562-b3fc-2c963f66afa6/approve
    /// 
    /// Response:
    /// 
    ///     {
    ///         "isSuccess": true,
    ///         "message": "Location request approved successfully"
    ///     }
    /// 
    /// This will:
    /// 1. Create a new Location entity (or link to existing if found)
    /// 2. Update the LocationRequest status to Approved
    /// 3. Set ReviewedBy to the current admin user ID
    /// 4. Set ReviewedAt to current timestamp
    /// 5. Link the created LocationId
    /// 
    /// </remarks>
    /// <param name="id">Location request ID to approve</param>
    /// <returns>Success result</returns>
    /// <response code="200">Location request approved successfully</response>
    /// <response code="400">Request not found or already processed</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not an Admin</response>
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveLocationRequest(Guid id)
    {
        _logger.LogInformation("Approving location request {LocationRequestId}", id);

        var command = new ApproveLocationRequestCommand(id);
        var result = await _mediator.Send(command);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Reject a location request with reason (Admin only)
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/LocationRequests/3fa85f64-5717-4562-b3fc-2c963f66afa6/reject
    ///     {
    ///         "rejectionReason": "This location already exists in our system as 'Mumbai - Marine Drive'. Please use the existing location when creating your activity."
    ///     }
    /// 
    /// Response:
    /// 
    ///     {
    ///         "isSuccess": true,
    ///         "message": "Location request rejected successfully"
    ///     }
    /// 
    /// This will:
    /// 1. Update the LocationRequest status to Rejected
    /// 2. Store the rejection reason for provider review
    /// 3. Set ReviewedBy to the current admin user ID
    /// 4. Set ReviewedAt to current timestamp
    /// 
    /// The provider can view the rejection reason and resubmit with corrections if needed.
    /// 
    /// </remarks>
    /// <param name="id">Location request ID to reject</param>
    /// <param name="command">Rejection details including reason</param>
    /// <returns>Success result</returns>
    /// <response code="200">Location request rejected successfully</response>
    /// <response code="400">Request not found, already processed, or invalid reason</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not an Admin</response>
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectLocationRequest(Guid id, [FromBody] RejectLocationRequestRequest request)
    {
        _logger.LogInformation("Rejecting location request {LocationRequestId}", id);

        var command = new RejectLocationRequestCommand(id, request.RejectionReason);
        var result = await _mediator.Send(command);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
}

/// <summary>
/// Request body for rejecting a location request
/// </summary>
public record RejectLocationRequestRequest(string RejectionReason);
