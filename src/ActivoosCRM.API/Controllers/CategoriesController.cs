using ActivoosCRM.Application.Features.Categories.Commands.CreateCategory;
using ActivoosCRM.Application.Features.Categories.Queries.GetCategories;
using ActivoosCRM.Application.Features.Categories.Queries.GetCategoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Category management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(IMediator mediator, ILogger<CategoriesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all activity categories
    /// </summary>
    /// <param name="includeInactive">Include inactive categories in results</param>
    /// <param name="parentOnly">Return only top-level categories (no parent)</param>
    /// <returns>List of categories with subcategories</returns>
    /// <response code="200">Returns list of categories</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] bool includeInactive = false,
        [FromQuery] bool parentOnly = false)
    {
        _logger.LogInformation("GET /api/Categories called with includeInactive={IncludeInactive}, parentOnly={ParentOnly}",
            includeInactive, parentOnly);

        var query = new GetCategoriesQuery
        {
            IncludeInactive = includeInactive,
            ParentOnly = parentOnly
        };

        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            data = result.Data
        });
    }

    /// <summary>
    /// Get category details by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Detailed category information including subcategories and activity count</returns>
    /// <response code="200">Returns category details</response>
    /// <response code="404">Category not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        _logger.LogInformation("GET /api/Categories/{Id} called", id);

        var query = new GetCategoryByIdQuery { CategoryId = id };
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return NotFound(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            data = result.Data
        });
    }

    /// <summary>
    /// Create a new category (Admin only)
    /// </summary>
    /// <param name="command">Category creation details</param>
    /// <returns>Created category ID</returns>
    /// <response code="201">Category created successfully</response>
    /// <response code="400">Invalid request or slug already exists</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="403">Forbidden - admin role required</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/Categories
    ///     {
    ///         "name": "Water Sports",
    ///         "slug": "water-sports",
    ///         "description": "Exciting water-based activities",
    ///         "iconUrl": "https://example.com/icons/water-sports.svg",
    ///         "displayOrder": 1,
    ///         "parentCategoryId": null
    ///     }
    /// 
    /// Only administrators can create categories.
    /// The slug must be unique and contain only lowercase letters, numbers, and hyphens.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        _logger.LogInformation("POST /api/Categories called for category: {Name}", command.Name);

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return CreatedAtAction(
            nameof(GetCategoryById),
            new { id = result.Data },
            new
            {
                success = true,
                message = "Category created successfully",
                data = new { categoryId = result.Data }
            });
    }
}
