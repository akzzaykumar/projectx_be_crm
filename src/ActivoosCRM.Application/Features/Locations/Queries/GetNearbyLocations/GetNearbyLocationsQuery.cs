using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Locations.Queries.GetNearbyLocations;

/// <summary>
/// Query to get locations near user's GPS coordinates
/// </summary>
public class GetNearbyLocationsQuery : IRequest<Result<List<NearbyLocationDto>>>
{
    /// <summary>
    /// User's current latitude
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// User's current longitude
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Search radius in kilometers (default: 50 km)
    /// </summary>
    public int RadiusKm { get; set; } = 50;

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; set; } = 10;
}

/// <summary>
/// Nearby location DTO with distance information
/// </summary>
public class NearbyLocationDto
{
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    /// <summary>
    /// Distance from user's location in kilometers
    /// </summary>
    public double DistanceKm { get; set; }

    public int TotalActivities { get; set; }
    public int TotalProviders { get; set; }
}
