using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Locations.Queries.GetNearbyLocations;

/// <summary>
/// Handler for GetNearbyLocationsQuery
/// Uses Haversine formula to calculate distances
/// </summary>
public class GetNearbyLocationsQueryHandler : IRequestHandler<GetNearbyLocationsQuery, Result<List<NearbyLocationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetNearbyLocationsQueryHandler> _logger;

    public GetNearbyLocationsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetNearbyLocationsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<NearbyLocationDto>>> Handle(GetNearbyLocationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Finding locations near coordinates: Lat={Latitude}, Lon={Longitude}, Radius={RadiusKm}km",
                request.Latitude, request.Longitude, request.RadiusKm);

            // Validate coordinates
            if (request.Latitude < -90 || request.Latitude > 90)
            {
                return Result<List<NearbyLocationDto>>.CreateFailure("Latitude must be between -90 and 90");
            }

            if (request.Longitude < -180 || request.Longitude > 180)
            {
                return Result<List<NearbyLocationDto>>.CreateFailure("Longitude must be between -180 and 180");
            }

            // Get all active locations with coordinates
            var locations = await _context.Locations
                .Include(l => l.Activities)
                .Where(l => l.IsActive && l.Latitude.HasValue && l.Longitude.HasValue)
                .ToListAsync(cancellationToken);

            // Calculate distances and filter by radius
            var nearbyLocations = locations
                .Select(l => new
                {
                    Location = l,
                    Distance = CalculateDistance(
                        request.Latitude, request.Longitude,
                        l.Latitude!.Value, l.Longitude!.Value)
                })
                .Where(x => x.Distance <= request.RadiusKm)
                .OrderBy(x => x.Distance)
                .Take(request.MaxResults)
                .Select(x => new NearbyLocationDto
                {
                    LocationId = x.Location.Id,
                    Name = x.Location.Name,
                    City = x.Location.City,
                    State = x.Location.State,
                    Country = x.Location.Country,
                    Latitude = x.Location.Latitude!.Value,
                    Longitude = x.Location.Longitude!.Value,
                    DistanceKm = Math.Round(x.Distance, 2),
                    TotalActivities = x.Location.Activities.Count(a => a.IsActive),
                    TotalProviders = x.Location.Activities
                        .Where(a => a.IsActive)
                        .Select(a => a.ProviderId)
                        .Distinct()
                        .Count()
                })
                .ToList();

            _logger.LogInformation("Found {Count} locations within {RadiusKm}km", nearbyLocations.Count, request.RadiusKm);

            return Result<List<NearbyLocationDto>>.CreateSuccess(nearbyLocations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding nearby locations");
            return Result<List<NearbyLocationDto>>.CreateFailure("Failed to find nearby locations");
        }
    }

    /// <summary>
    /// Calculate distance between two GPS coordinates using Haversine formula
    /// </summary>
    /// <param name="lat1">Latitude of point 1</param>
    /// <param name="lon1">Longitude of point 1</param>
    /// <param name="lat2">Latitude of point 2</param>
    /// <param name="lon2">Longitude of point 2</param>
    /// <returns>Distance in kilometers</returns>
    private static double CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double EarthRadiusKm = 6371.0;

        // Convert to radians
        var dLat = DegreesToRadians((double)(lat2 - lat1));
        var dLon = DegreesToRadians((double)(lon2 - lon1));

        var lat1Rad = DegreesToRadians((double)lat1);
        var lat2Rad = DegreesToRadians((double)lat2);

        // Haversine formula
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
