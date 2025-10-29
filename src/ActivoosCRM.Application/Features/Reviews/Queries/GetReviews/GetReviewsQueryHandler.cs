using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Reviews.Queries.GetReviews;

/// <summary>
/// Handler for GetReviewsQuery
/// Retrieves reviews with filters, pagination, and rating distribution
/// </summary>
public class GetReviewsQueryHandler : IRequestHandler<GetReviewsQuery, Result<GetReviewsResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetReviewsQueryHandler> _logger;

    public GetReviewsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetReviewsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<GetReviewsResponse>> Handle(GetReviewsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Build query with filters
            var query = _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Activity)
                .Where(r => !r.IsDeleted)
                .AsQueryable();

            // Apply filters
            if (request.ActivityId.HasValue)
                query = query.Where(r => r.ActivityId == request.ActivityId.Value);

            if (request.ProviderId.HasValue)
                query = query.Where(r => r.ProviderId == request.ProviderId.Value);

            if (request.CustomerId.HasValue)
                query = query.Where(r => r.CustomerId == request.CustomerId.Value);

            if (request.Rating.HasValue)
                query = query.Where(r => r.Rating == request.Rating.Value);

            if (request.IsVerified.HasValue)
                query = query.Where(r => r.IsVerified == request.IsVerified.Value);

            // Calculate total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Calculate average rating
            var averageRating = totalCount > 0
                ? await query.AverageAsync(r => (decimal)r.Rating, cancellationToken)
                : 0m;

            // Calculate rating distribution
            var ratingDistribution = await query
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Rating, x => x.Count, cancellationToken);

            // Ensure all ratings 1-5 are present in distribution
            for (int i = 1; i <= 5; i++)
            {
                if (!ratingDistribution.ContainsKey(i))
                    ratingDistribution[i] = 0;
            }

            // Apply pagination
            var pageSize = Math.Min(request.PageSize, 50); // Max 50 items per page
            var skip = (request.Page - 1) * pageSize;

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(r => new ReviewDto
                {
                    ReviewId = r.Id,
                    Rating = r.Rating,
                    Title = r.Title,
                    ReviewText = r.ReviewText,
                    IsVerified = r.IsVerified,
                    IsFeatured = r.IsFeatured,
                    HelpfulCount = r.HelpfulCount,
                    Customer = new ReviewCustomerDto
                    {
                        FirstName = r.Customer.FirstName,
                        LastName = r.Customer.LastName
                    },
                    Activity = new ReviewActivityDto
                    {
                        ActivityId = r.Activity.Id,
                        Title = r.Activity.Title
                    },
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var response = new GetReviewsResponse
            {
                Items = reviews,
                TotalCount = totalCount,
                PageNumber = request.Page,
                PageSize = pageSize,
                TotalPages = totalPages,
                AverageRating = Math.Round(averageRating, 1),
                RatingDistribution = ratingDistribution
            };

            _logger.LogInformation("Retrieved {Count} reviews (page {Page} of {TotalPages})",
                reviews.Count, request.Page, totalPages);

            return Result<GetReviewsResponse>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reviews");
            return Result<GetReviewsResponse>.CreateFailure("Failed to retrieve reviews");
        }
    }
}
