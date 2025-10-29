using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Activities.Queries.GetActivities;

/// <summary>
/// Query to get activities with comprehensive filters and pagination
/// </summary>
public record GetActivitiesQuery : IRequest<Result<PaginatedList<ActivityDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? LocationId { get; init; }
    public Guid? ProviderId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public decimal? MinRating { get; init; }
    public string? DifficultyLevel { get; init; }
    public string? SortBy { get; init; } = "newest"; // price, rating, popularity, newest
    public string? SortOrder { get; init; } = "desc"; // asc, desc
    public bool? Featured { get; init; }
}

/// <summary>
/// Activity DTO for list view
/// </summary>
public record ActivityDto
{
    public Guid ActivityId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? CoverImageUrl { get; init; }
    public decimal Price { get; init; }
    public decimal? DiscountedPrice { get; init; }
    public string Currency { get; init; } = "INR";
    public bool HasActiveDiscount { get; init; }
    public int DurationMinutes { get; init; }
    public int MaxParticipants { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public string? DifficultyLevel { get; init; }
    public CategorySummaryDto Category { get; init; } = null!;
    public LocationSummaryDto Location { get; init; } = null!;
    public ProviderSummaryDto Provider { get; init; } = null!;
    public bool IsFeatured { get; init; }
    public bool IsActive { get; init; }
}

public record CategorySummaryDto
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record LocationSummaryDto
{
    public Guid LocationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
}

public record ProviderSummaryDto
{
    public Guid ProviderId { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public decimal AverageRating { get; init; }
    public bool IsVerified { get; init; }
}
