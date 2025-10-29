using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// ActivityImage entity - Stores activity images/photos
/// Responsible for: Activity image gallery, primary image management, ordering
/// </summary>
public class ActivityImage : BaseEntity
{
    private ActivityImage() { } // Private constructor for EF Core

    // Relationships
    public Guid ActivityId { get; private set; }
    public virtual Activity Activity { get; private set; } = null!;

    // Image details
    public string ImageUrl { get; private set; } = string.Empty;
    public string? Caption { get; private set; }
    public bool IsPrimary { get; private set; } = false;
    public int SortOrder { get; private set; } = 0;

    /// <summary>
    /// Factory method to create a new activity image
    /// </summary>
    public static ActivityImage Create(
        Guid activityId,
        string imageUrl,
        string? caption = null,
        bool isPrimary = false,
        int sortOrder = 0)
    {
        if (activityId == Guid.Empty)
            throw new ArgumentException("Activity ID is required", nameof(activityId));

        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL is required", nameof(imageUrl));

        var image = new ActivityImage
        {
            Id = Guid.NewGuid(),
            ActivityId = activityId,
            ImageUrl = imageUrl.Trim(),
            Caption = caption?.Trim(),
            IsPrimary = isPrimary,
            SortOrder = sortOrder
        };

        return image;
    }

    /// <summary>
    /// Update image details
    /// </summary>
    public void Update(string? caption, int sortOrder)
    {
        Caption = caption?.Trim();
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Set as primary image
    /// </summary>
    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    /// <summary>
    /// Unset as primary image
    /// </summary>
    public void UnsetAsPrimary()
    {
        IsPrimary = false;
    }
}
