using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Locations.Queries.GetLocations;

/// <summary>
/// Query to get all locations with optional filters
/// </summary>
public class GetLocationsQuery : IRequest<Result<List<LocationDto>>>
{
    /// <summary>
    /// Search by name or city
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by country
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Filter by state
    /// </summary>
    public string? State { get; set; }
}

/// <summary>
/// Location DTO for list response
/// </summary>
public class LocationDto
{
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int TotalActivities { get; set; }
    public int TotalProviders { get; set; }
}
