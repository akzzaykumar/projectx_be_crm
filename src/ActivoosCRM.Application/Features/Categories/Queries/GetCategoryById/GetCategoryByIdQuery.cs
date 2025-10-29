using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Categories.Queries.GetCategoryById;

/// <summary>
/// Query to get category details by ID
/// </summary>
public class GetCategoryByIdQuery : IRequest<Result<CategoryDetailDto>>
{
    public Guid CategoryId { get; set; }
}

/// <summary>
/// Detailed category DTO
/// </summary>
public class CategoryDetailDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public ParentCategoryDto? ParentCategory { get; set; }
    public List<SubCategoryDetailDto> SubCategories { get; set; } = new();
    public int TotalActivities { get; set; }
}

/// <summary>
/// Parent category DTO
/// </summary>
public class ParentCategoryDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

/// <summary>
/// Sub-category detail DTO
/// </summary>
public class SubCategoryDetailDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
