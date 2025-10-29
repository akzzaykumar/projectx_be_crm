using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Reviews.Queries.GetReviews;

/// <summary>
/// Query to get reviews with filters and pagination
/// </summary>
public record GetReviewsQuery : IRequest<Result<GetReviewsResponse>>
{
    public Guid? ActivityId { get; init; }
    public Guid? ProviderId { get; init; }
    public Guid? CustomerId { get; init; }
    public int? Rating { get; init; }
    public bool? IsVerified { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

/// <summary>
/// Response containing paginated reviews with rating distribution
/// </summary>
public record GetReviewsResponse
{
    public List<ReviewDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public decimal AverageRating { get; init; }
    public Dictionary<int, int> RatingDistribution { get; init; } = new();
}

/// <summary>
/// Review data transfer object
/// </summary>
public record ReviewDto
{
    public Guid ReviewId { get; init; }
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? ReviewText { get; init; }
    public bool IsVerified { get; init; }
    public bool IsFeatured { get; init; }
    public int HelpfulCount { get; init; }
    public ReviewCustomerDto Customer { get; init; } = null!;
    public ReviewActivityDto Activity { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}

public record ReviewCustomerDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}

public record ReviewActivityDto
{
    public Guid ActivityId { get; init; }
    public string Title { get; init; } = string.Empty;
}
