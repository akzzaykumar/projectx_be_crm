using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Categories.Queries.GetCategories;

/// <summary>
/// Handler for GetCategoriesQuery
/// </summary>
public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, Result<List<CategoryDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetCategoriesQueryHandler> _logger;

    public GetCategoriesQueryHandler(
        IApplicationDbContext context,
        ILogger<GetCategoriesQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching categories with filters: IncludeInactive={IncludeInactive}, ParentOnly={ParentOnly}",
                request.IncludeInactive, request.ParentOnly);

            var query = _context.Categories.AsQueryable();

            // Filter by active status
            if (!request.IncludeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            // Filter by parent categories only
            if (request.ParentOnly)
            {
                query = query.Where(c => c.ParentCategoryId == null);
            }

            // Include subcategories
            query = query
                .Include(c => c.SubCategories.Where(sc => request.IncludeInactive || sc.IsActive))
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name);

            var categories = await query.ToListAsync(cancellationToken);

            var categoryDtos = categories.Select(c => new CategoryDto
            {
                CategoryId = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                IconUrl = c.IconUrl,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                ParentCategoryId = c.ParentCategoryId,
                SubCategories = c.SubCategories
                    .OrderBy(sc => sc.DisplayOrder)
                    .ThenBy(sc => sc.Name)
                    .Select(sc => new SubCategoryDto
                    {
                        CategoryId = sc.Id,
                        Name = sc.Name,
                        Slug = sc.Slug,
                        DisplayOrder = sc.DisplayOrder
                    }).ToList()
            }).ToList();

            _logger.LogInformation("Successfully fetched {Count} categories", categoryDtos.Count);

            return Result<List<CategoryDto>>.CreateSuccess(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching categories");
            return Result<List<CategoryDto>>.CreateFailure("Failed to fetch categories");
        }
    }
}
