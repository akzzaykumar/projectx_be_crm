using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.LocationRequests.Queries.GetLocationRequests;

public class GetLocationRequestsQueryHandler : IRequestHandler<GetLocationRequestsQuery, Result<PaginatedList<LocationRequestDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetLocationRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<LocationRequestDto>>> Handle(GetLocationRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LocationRequests
            .Include(lr => lr.Provider)
            .Include(lr => lr.Location)
            .AsQueryable();

        // Filter by status
        if (!string.IsNullOrEmpty(request.Status))
        {
            if (Enum.TryParse<LocationRequestStatus>(request.Status, true, out var status))
            {
                query = query.Where(lr => lr.Status == status);
            }
        }

        // Filter by provider
        if (request.ProviderId.HasValue)
        {
            query = query.Where(lr => lr.ProviderId == request.ProviderId.Value);
        }

        // Order by created date (newest first)
        query = query.OrderByDescending(lr => lr.CreatedAt);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var pageSize = Math.Min(request.PageSize, 50); // Max 50 items per page
        var items = await query
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .Select(lr => new LocationRequestDto
            {
                Id = lr.Id,
                Name = lr.Name,
                City = lr.City,
                State = lr.State,
                Country = lr.Country,
                Address = lr.Address,
                Latitude = lr.Latitude,
                Longitude = lr.Longitude,
                Reason = lr.Reason,
                Status = lr.Status.ToString(),
                CreatedAt = lr.CreatedAt,
                ReviewedAt = lr.ReviewedAt,
                RejectionReason = lr.RejectionReason,
                LocationId = lr.LocationId,

                Provider = new ProviderSummary
                {
                    Id = lr.Provider.Id,
                    BusinessName = lr.Provider.BusinessName,
                    ContactEmail = lr.Provider.BusinessEmail
                },

                CreatedLocation = lr.Location != null ? new LocationSummary
                {
                    Id = lr.Location.Id,
                    Name = lr.Location.Name,
                    City = lr.Location.City
                } : null
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<LocationRequestDto>(
            items,
            totalCount,
            request.Page,
            pageSize);

        return Result<PaginatedList<LocationRequestDto>>.CreateSuccess(paginatedList);
    }
}
