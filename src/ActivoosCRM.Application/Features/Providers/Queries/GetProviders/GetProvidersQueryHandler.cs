using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Providers.Queries.GetProviders;

/// <summary>
/// Handler for GetProvidersQuery
/// </summary>
public class GetProvidersQueryHandler : IRequestHandler<GetProvidersQuery, Result<PaginatedResult<ProviderListItemDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetProvidersQueryHandler> _logger;

    public GetProvidersQueryHandler(
        IApplicationDbContext context,
        ILogger<GetProvidersQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<ProviderListItemDto>>> Handle(
        GetProvidersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching providers with filters - Page: {Page}, PageSize: {PageSize}",
                request.Page, request.PageSize);

            // Validate pagination
            if (request.Page < 1)
                request.Page = 1;

            if (request.PageSize < 1 || request.PageSize > 50)
                request.PageSize = 10;

            // Build query
            var query = _context.ActivityProviders
                .Include(p => p.Location)
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(p =>
                    p.BusinessName.ToLower().Contains(searchLower) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)));
            }

            if (request.LocationId.HasValue)
            {
                query = query.Where(p => p.LocationId == request.LocationId.Value);
            }

            if (request.IsVerified.HasValue)
            {
                query = query.Where(p => p.IsVerified == request.IsVerified.Value);
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "rating" => request.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.AverageRating)
                    : query.OrderBy(p => p.AverageRating),
                "createdat" => request.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt),
                _ => request.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.BusinessName)
                    : query.OrderBy(p => p.BusinessName)
            };

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProviderListItemDto
                {
                    ProviderId = p.Id,
                    BusinessName = p.BusinessName,
                    Description = p.Description,
                    LogoUrl = p.LogoUrl,
                    Location = p.Location != null ? new LocationDto
                    {
                        LocationId = p.Location.Id,
                        Name = p.Location.Name,
                        City = p.Location.City,
                        State = p.Location.State
                    } : null,
                    AverageRating = p.AverageRating,
                    TotalReviews = p.TotalReviews,
                    TotalBookings = p.TotalBookings,
                    IsVerified = p.IsVerified,
                    IsActive = p.IsActive
                })
                .ToListAsync(cancellationToken);

            var result = new PaginatedResult<ProviderListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };

            _logger.LogInformation("Retrieved {Count} providers out of {Total}", items.Count, totalCount);

            return Result<PaginatedResult<ProviderListItemDto>>.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching providers");
            return Result<PaginatedResult<ProviderListItemDto>>.CreateFailure(
                "An error occurred while fetching providers");
        }
    }
}
