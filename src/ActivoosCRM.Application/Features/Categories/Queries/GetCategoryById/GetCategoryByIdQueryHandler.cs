using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Categories.Queries.GetCategoryById;

/// <summary>
/// Handler for GetCategoryByIdQuery
/// </summary>
public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetCategoryByIdQueryHandler> _logger;

    public GetCategoryByIdQueryHandler(
        IApplicationDbContext context,
        ILogger<GetCategoryByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CategoryDetailDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching category details for CategoryId: {CategoryId}", request.CategoryId);

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null)
            {
                _logger.LogWarning("Category not found with CategoryId: {CategoryId}", request.CategoryId);
                return Result<CategoryDetailDto>.CreateFailure("Category not found");
            }

            var categoryDto = new CategoryDetailDto
            {
                CategoryId = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                IconUrl = category.IconUrl,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                ParentCategory = category.ParentCategory != null
                    ? new ParentCategoryDto
                    {
                        CategoryId = category.ParentCategory.Id,
                        Name = category.ParentCategory.Name,
                        Slug = category.ParentCategory.Slug
                    }
                    : null,
                SubCategories = category.SubCategories
                    .Select(sc => new SubCategoryDetailDto
                    {
                        CategoryId = sc.Id,
                        Name = sc.Name,
                        Slug = sc.Slug
                    }).ToList(),
                TotalActivities = category.Activities.Count(a => a.IsActive)
            };

            _logger.LogInformation("Successfully fetched category details for CategoryId: {CategoryId}", request.CategoryId);

            return Result<CategoryDetailDto>.CreateSuccess(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category details for CategoryId: {CategoryId}", request.CategoryId);
            return Result<CategoryDetailDto>.CreateFailure("Failed to fetch category details");
        }
    }
}
