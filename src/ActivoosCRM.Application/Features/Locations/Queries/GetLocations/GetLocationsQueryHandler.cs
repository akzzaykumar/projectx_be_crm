using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Locations.Queries.GetLocations;

/// <summary>
/// Handler for GetLocationsQuery
/// </summary>
public class GetLocationsQueryHandler : IRequestHandler<GetLocationsQuery, Result<List<LocationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetLocationsQueryHandler> _logger;

    public GetLocationsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetLocationsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<LocationDto>>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching locations with filters: Search={Search}, Country={Country}, State={State}",
                request.Search, request.Country, request.State);

            var query = _context.Locations
                .Include(l => l.Activities)
                .Where(l => l.IsActive)
                .AsQueryable();

            // Search filter - search in name or city
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.ToLower();
                query = query.Where(l =>
                    l.Name.ToLower().Contains(searchTerm) ||
                    l.City.ToLower().Contains(searchTerm));
            }

            // Country filter
            if (!string.IsNullOrWhiteSpace(request.Country))
            {
                query = query.Where(l => l.Country.ToLower() == request.Country.ToLower());
            }

            // State filter
            if (!string.IsNullOrWhiteSpace(request.State))
            {
                query = query.Where(l => l.State.ToLower() == request.State.ToLower());
            }

            // Order by name
            query = query.OrderBy(l => l.Name);

            var locations = await query.ToListAsync(cancellationToken);

            var locationDtos = locations.Select(l => new LocationDto
            {
                LocationId = l.Id,
                Name = l.Name,
                City = l.City,
                State = l.State,
                Country = l.Country,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                TotalActivities = l.Activities.Count(a => a.IsActive),
                TotalProviders = l.Activities
                    .Where(a => a.IsActive)
                    .Select(a => a.ProviderId)
                    .Distinct()
                    .Count()
            }).ToList();

            _logger.LogInformation("Successfully fetched {Count} locations", locationDtos.Count);

            return Result<List<LocationDto>>.CreateSuccess(locationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching locations");
            return Result<List<LocationDto>>.CreateFailure("Failed to fetch locations");
        }
    }
}
