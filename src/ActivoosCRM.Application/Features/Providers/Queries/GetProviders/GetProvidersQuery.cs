using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Providers.Queries.GetProviders;

/// <summary>
/// Query to get list of activity providers with filters
/// </summary>
public class GetProvidersQuery : IRequest<Result<PaginatedResult<ProviderListItemDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public Guid? LocationId { get; set; }
    public bool? IsVerified { get; set; }
    public string? SortBy { get; set; } = "name"; // rating, name, createdAt
    public string? SortOrder { get; set; } = "asc"; // asc, desc
}

/// <summary>
/// Provider list item DTO
/// </summary>
public class ProviderListItemDto
{
    public Guid ProviderId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public LocationDto? Location { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalBookings { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Location DTO
/// </summary>
public class LocationDto
{
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}
