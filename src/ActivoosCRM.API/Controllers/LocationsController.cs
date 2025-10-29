using ActivoosCRM.Application.Features.Locations.Queries.GetLocations;
using ActivoosCRM.Application.Features.Locations.Queries.GetNearbyLocations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Location management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LocationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(IMediator mediator, ILogger<LocationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all locations
    /// </summary>
    /// <param name="search">Search by name or city</param>
    /// <param name="country">Filter by country</param>
    /// <param name="state">Filter by state</param>
    /// <returns>List of locations with activity and provider counts</returns>
    /// <response code="200">Returns list of locations</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/Locations
    ///     GET /api/Locations?search=Goa
    ///     GET /api/Locations?country=India&amp;state=Goa
    /// 
    /// Returns active locations with statistics about total activities and providers.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocations(
        [FromQuery] string? search = null,
        [FromQuery] string? country = null,
        [FromQuery] string? state = null)
    {
        _logger.LogInformation("GET /api/Locations called with search={Search}, country={Country}, state={State}",
            search, country, state);

        var query = new GetLocationsQuery
        {
            Search = search,
            Country = country,
            State = state
        };

        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            data = result.Data
        });
    }

    /// <summary>
    /// Get locations near user's current GPS coordinates
    /// </summary>
    /// <param name="latitude">User's current latitude</param>
    /// <param name="longitude">User's current longitude</param>
    /// <param name="radiusKm">Search radius in kilometers (default: 50 km)</param>
    /// <param name="maxResults">Maximum number of results (default: 10)</param>
    /// <returns>List of nearby locations sorted by distance</returns>
    /// <response code="200">Returns nearby locations with distance information</response>
    /// <response code="400">Invalid coordinates</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/Locations/nearby?latitude=19.0760&amp;longitude=72.8777&amp;radiusKm=50
    ///     
    /// This endpoint uses the Haversine formula to calculate distances between GPS coordinates.
    /// Returns locations sorted by distance (nearest first) with activity and provider counts.
    /// 
    /// **Use Cases:**
    /// - Show activities near user's current location
    /// - Location-based recommendations
    /// - Find nearest service providers
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": [
    ///     {
    ///       "locationId": "guid",
    ///       "name": "Goa",
    ///       "city": "Panaji",
    ///       "distanceKm": 12.5,
    ///       "totalActivities": 45
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNearbyLocations(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        [FromQuery] int radiusKm = 50,
        [FromQuery] int maxResults = 10)
    {
        _logger.LogInformation(
            "GET /api/Locations/nearby called with latitude={Latitude}, longitude={Longitude}, radiusKm={RadiusKm}",
            latitude, longitude, radiusKm);

        var query = new GetNearbyLocationsQuery
        {
            Latitude = latitude,
            Longitude = longitude,
            RadiusKm = radiusKm,
            MaxResults = maxResults
        };

        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            data = result.Data
        });
    }
}
