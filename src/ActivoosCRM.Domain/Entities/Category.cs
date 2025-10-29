using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Category entity - Represents activity categories for classification
/// Responsible for: Activity categorization, hierarchy management
/// </summary>
public class Category : AuditableEntity
{
    private Category() { } // Private constructor for EF Core

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Hierarchy support
    public Guid? ParentCategoryId { get; private set; }
    public virtual Category? ParentCategory { get; private set; }
    public virtual ICollection<Category> SubCategories { get; private set; } = new List<Category>();

    // Navigation properties
    public virtual ICollection<Activity> Activities { get; private set; } = new List<Activity>();

    /// <summary>
    /// Factory method to create a new category
    /// </summary>
    public static Category Create(
        string name,
        string slug,
        string? description = null,
        string? iconUrl = null,
        int displayOrder = 0,
        Guid? parentCategoryId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Category slug is required", nameof(slug));

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug.ToLowerInvariant().Trim(),
            Description = description?.Trim(),
            IconUrl = iconUrl?.Trim(),
            DisplayOrder = displayOrder,
            ParentCategoryId = parentCategoryId,
            IsActive = true
        };

        return category;
    }

    /// <summary>
    /// Update category details
    /// </summary>
    public void Update(string name, string? description, string? iconUrl, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        IconUrl = iconUrl?.Trim();
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Activate category
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivate category
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Set parent category
    /// </summary>
    public void SetParent(Guid? parentCategoryId)
    {
        // Prevent circular reference
        if (parentCategoryId.HasValue && parentCategoryId.Value == Id)
            throw new InvalidOperationException("Category cannot be its own parent");

        ParentCategoryId = parentCategoryId;
    }

    /// <summary>
    /// Check if category is top-level
    /// </summary>
    public bool IsTopLevel()
    {
        return !ParentCategoryId.HasValue;
    }
}
