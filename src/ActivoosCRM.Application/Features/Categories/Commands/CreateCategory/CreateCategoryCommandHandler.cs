using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Categories.Commands.CreateCategory;

/// <summary>
/// Handler for CreateCategoryCommand
/// </summary>
public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        IApplicationDbContext context,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating new category: {Name}", request.Name);

            // Check if slug already exists
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == request.Slug.ToLowerInvariant(), cancellationToken);

            if (existingCategory != null)
            {
                _logger.LogWarning("Category with slug '{Slug}' already exists", request.Slug);
                return Result<Guid>.CreateFailure($"A category with slug '{request.Slug}' already exists");
            }

            // Validate parent category exists if provided
            if (request.ParentCategoryId.HasValue)
            {
                var parentExists = await _context.Categories
                    .AnyAsync(c => c.Id == request.ParentCategoryId.Value, cancellationToken);

                if (!parentExists)
                {
                    _logger.LogWarning("Parent category not found: {ParentCategoryId}", request.ParentCategoryId.Value);
                    return Result<Guid>.CreateFailure("Parent category not found");
                }
            }

            // Create category using factory method
            var category = Category.Create(
                name: request.Name,
                slug: request.Slug,
                description: request.Description,
                iconUrl: request.IconUrl,
                displayOrder: request.DisplayOrder,
                parentCategoryId: request.ParentCategoryId
            );

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created category with Id: {CategoryId}", category.Id);

            return Result<Guid>.CreateSuccess(category.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return Result<Guid>.CreateFailure("Failed to create category");
        }
    }
}
