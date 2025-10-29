using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Categories.Queries.GetCategories;

/// <summary>
/// Query to get all categories with optional filters
/// </summary>
public class GetCategoriesQuery : IRequest<Result<List<CategoryDto>>>
{
    /// <summary>
    /// Include inactive categories in results
    /// </summary>
    public bool IncludeInactive { get; set; } = false;

    /// <summary>
    /// Return only top-level categories (no parent)
    /// </summary>
    public bool ParentOnly { get; set; } = false;
}

/// <summary>
/// Category DTO for list response
/// </summary>
public class CategoryDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public List<SubCategoryDto> SubCategories { get; set; } = new();
}

/// <summary>
/// Sub-category DTO for nested structure
/// </summary>
public class SubCategoryDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
