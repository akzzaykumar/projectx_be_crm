using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Wishlist entity - Represents customer's favorite activities
/// Responsible for: Tracking customer wishlists/favorites
/// </summary>
public class Wishlist : BaseEntity
{
    private Wishlist() { } // Private constructor for EF Core

    // Relationships
    public Guid CustomerId { get; private set; }
    public virtual User Customer { get; private set; } = null!;

    public Guid ActivityId { get; private set; }
    public virtual Activity Activity { get; private set; } = null!;

    /// <summary>
    /// Factory method to create a new wishlist entry
    /// </summary>
    public static Wishlist Create(Guid customerId, Guid activityId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required", nameof(customerId));

        if (activityId == Guid.Empty)
            throw new ArgumentException("Activity ID is required", nameof(activityId));

        var wishlist = new Wishlist
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ActivityId = activityId
        };

        return wishlist;
    }
}
