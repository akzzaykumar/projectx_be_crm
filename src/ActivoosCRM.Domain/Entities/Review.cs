using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Review entity - Represents customer reviews and ratings for activities
/// Responsible for: Review content, ratings, verification, helpfulness tracking
/// </summary>
public class Review : AuditableEntity
{
    private Review() { } // Private constructor for EF Core

    // Relationships
    public Guid BookingId { get; private set; }
    public virtual Booking Booking { get; private set; } = null!;

    public Guid CustomerId { get; private set; }
    public virtual User Customer { get; private set; } = null!;

    public Guid ActivityId { get; private set; }
    public virtual Activity Activity { get; private set; } = null!;

    public Guid ProviderId { get; private set; }
    public virtual ActivityProvider Provider { get; private set; } = null!;

    // Review content
    public int Rating { get; private set; } // 1-5
    public string? Title { get; private set; }
    public string? ReviewText { get; private set; }

    // Status
    public bool IsVerified { get; private set; } = true;
    public bool IsFeatured { get; private set; } = false;
    public int HelpfulCount { get; private set; } = 0;

    /// <summary>
    /// Factory method to create a new review
    /// </summary>
    public static Review Create(
        Guid bookingId,
        Guid customerId,
        Guid activityId,
        Guid providerId,
        int rating,
        string? title = null,
        string? reviewText = null)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("Booking ID is required", nameof(bookingId));

        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required", nameof(customerId));

        if (activityId == Guid.Empty)
            throw new ArgumentException("Activity ID is required", nameof(activityId));

        if (providerId == Guid.Empty)
            throw new ArgumentException("Provider ID is required", nameof(providerId));

        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

        var review = new Review
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            CustomerId = customerId,
            ActivityId = activityId,
            ProviderId = providerId,
            Rating = rating,
            Title = title?.Trim(),
            ReviewText = reviewText?.Trim(),
            IsVerified = true,
            IsFeatured = false,
            HelpfulCount = 0
        };

        return review;
    }

    /// <summary>
    /// Update review content
    /// </summary>
    public void Update(int rating, string? title, string? reviewText)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

        Rating = rating;
        Title = title?.Trim();
        ReviewText = reviewText?.Trim();
    }

    /// <summary>
    /// Mark review as featured
    /// </summary>
    public void Feature()
    {
        IsFeatured = true;
    }

    /// <summary>
    /// Unmark review as featured
    /// </summary>
    public void Unfeature()
    {
        IsFeatured = false;
    }

    /// <summary>
    /// Increment helpful count
    /// </summary>
    public void IncrementHelpfulCount()
    {
        HelpfulCount++;
    }

    /// <summary>
    /// Mark review as verified
    /// </summary>
    public void MarkAsVerified()
    {
        IsVerified = true;
    }

    /// <summary>
    /// Mark review as unverified
    /// </summary>
    public void MarkAsUnverified()
    {
        IsVerified = false;
    }
}
