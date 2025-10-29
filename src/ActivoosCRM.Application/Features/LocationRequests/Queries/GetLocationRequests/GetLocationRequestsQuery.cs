using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.LocationRequests.Queries.GetLocationRequests;

public record GetLocationRequestsQuery : IRequest<Result<PaginatedList<LocationRequestDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Status { get; init; }  // Pending, Approved, Rejected
    public Guid? ProviderId { get; init; }
}

public record LocationRequestDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? Address { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? Reason { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public Guid? LocationId { get; init; }

    public ProviderSummary Provider { get; init; } = null!;
    public LocationSummary? CreatedLocation { get; init; }
}

public record ProviderSummary
{
    public Guid Id { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string? ContactEmail { get; init; }
}

public record LocationSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
}
