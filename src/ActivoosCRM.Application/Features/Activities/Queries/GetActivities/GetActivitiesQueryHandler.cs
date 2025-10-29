using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Activities.Queries.GetActivities;

/// <summary>
/// Handler for GetActivitiesQuery
/// </summary>
public class GetActivitiesQueryHandler : IRequestHandler<GetActivitiesQuery, Result<PaginatedList<ActivityDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetActivitiesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<ActivityDto>>> Handle(
        GetActivitiesQuery request,
        CancellationToken cancellationToken)
    {
        // Validate page size (max 50)
        var pageSize = Math.Min(request.PageSize, 50);

        // Start with base query - only active activities for public access
        var query = _context.Activities
            .Include(a => a.Category)
            .Include(a => a.Location)
            .Include(a => a.Provider)
            .Where(a => a.IsActive)
            .AsQueryable();

        // Apply search filter (title or description)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(searchTerm) ||
                (a.Description != null && a.Description.ToLower().Contains(searchTerm)) ||
                (a.ShortDescription != null && a.ShortDescription.ToLower().Contains(searchTerm)));
        }

        // Apply category filter
        if (request.CategoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == request.CategoryId.Value);
        }

        // Apply location filter
        if (request.LocationId.HasValue)
        {
            query = query.Where(a => a.LocationId == request.LocationId.Value);
        }

        // Apply provider filter
        if (request.ProviderId.HasValue)
        {
            query = query.Where(a => a.ProviderId == request.ProviderId.Value);
        }

        // Apply price range filters
        if (request.MinPrice.HasValue)
        {
            query = query.Where(a => a.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(a => a.Price <= request.MaxPrice.Value);
        }

        // Apply rating filter
        if (request.MinRating.HasValue)
        {
            query = query.Where(a => a.AverageRating >= request.MinRating.Value);
        }

        // Apply difficulty level filter
        if (!string.IsNullOrWhiteSpace(request.DifficultyLevel))
        {
            var difficulty = request.DifficultyLevel.ToLower();
            query = query.Where(a => a.DifficultyLevel != null && a.DifficultyLevel.ToLower() == difficulty);
        }

        // Apply featured filter
        if (request.Featured.HasValue)
        {
            query = query.Where(a => a.IsFeatured == request.Featured.Value);
        }

        // Apply sorting
        query = ApplySorting(query, request.SortBy?.ToLower(), request.SortOrder?.ToLower());

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var activities = await query
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityDto
            {
                ActivityId = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                ShortDescription = a.ShortDescription,
                CoverImageUrl = a.CoverImageUrl,
                Price = a.Price,
                DiscountedPrice = a.DiscountedPrice,
                Currency = a.Currency,
                HasActiveDiscount = a.HasActiveDiscount,
                DurationMinutes = a.DurationMinutes,
                MaxParticipants = a.MaxParticipants,
                AverageRating = a.AverageRating,
                TotalReviews = a.TotalReviews,
                DifficultyLevel = a.DifficultyLevel,
                Category = new CategorySummaryDto
                {
                    CategoryId = a.Category.Id,
                    Name = a.Category.Name
                },
                Location = new LocationSummaryDto
                {
                    LocationId = a.Location.Id,
                    Name = a.Location.Name,
                    City = a.Location.City
                },
                Provider = new ProviderSummaryDto
                {
                    ProviderId = a.Provider.Id,
                    BusinessName = a.Provider.BusinessName,
                    AverageRating = a.Provider.AverageRating,
                    IsVerified = a.Provider.IsVerified
                },
                IsFeatured = a.IsFeatured,
                IsActive = a.IsActive
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<ActivityDto>(
            activities,
            totalCount,
            request.Page,
            pageSize);

        return Result<PaginatedList<ActivityDto>>.CreateSuccess(paginatedList);
    }

    private IQueryable<Domain.Entities.Activity> ApplySorting(
        IQueryable<Domain.Entities.Activity> query,
        string? sortBy,
        string? sortOrder)
    {
        var isDescending = sortOrder != "asc";

        return sortBy switch
        {
            "price" => isDescending
                ? query.OrderByDescending(a => a.Price)
                : query.OrderBy(a => a.Price),
            "rating" => isDescending
                ? query.OrderByDescending(a => a.AverageRating).ThenByDescending(a => a.TotalReviews)
                : query.OrderBy(a => a.AverageRating).ThenBy(a => a.TotalReviews),
            "popularity" => isDescending
                ? query.OrderByDescending(a => a.TotalBookings).ThenByDescending(a => a.ViewCount)
                : query.OrderBy(a => a.TotalBookings).ThenBy(a => a.ViewCount),
            "newest" => isDescending
                ? query.OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
                : query.OrderBy(a => a.PublishedAt ?? a.CreatedAt),
            _ => query.OrderByDescending(a => a.PublishedAt ?? a.CreatedAt) // Default: newest first
        };
    }
}
