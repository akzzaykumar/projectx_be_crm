using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// ActivityTag entity - Tags for better activity searchability and categorization
/// Responsible for: Activity tagging, search optimization
/// </summary>
public class ActivityTag : BaseEntity
{
    private ActivityTag() { } // Private constructor for EF Core

    // Relationships
    public Guid ActivityId { get; private set; }
    public virtual Activity Activity { get; private set; } = null!;

    // Tag details
    public string Tag { get; private set; } = string.Empty;

    /// <summary>
    /// Factory method to create a new activity tag
    /// </summary>
    public static ActivityTag Create(Guid activityId, string tag)
    {
        if (activityId == Guid.Empty)
            throw new ArgumentException("Activity ID is required", nameof(activityId));

        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag is required", nameof(tag));

        var activityTag = new ActivityTag
        {
            Id = Guid.NewGuid(),
            ActivityId = activityId,
            Tag = tag.ToLowerInvariant().Trim()
        };

        return activityTag;
    }

    /// <summary>
    /// Update tag value
    /// </summary>
    public void UpdateTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag is required", nameof(tag));

        Tag = tag.ToLowerInvariant().Trim();
    }
}
