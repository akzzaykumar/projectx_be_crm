using ActivoosCRM.Domain.Common;
using ActivoosCRM.Domain.Enums;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Activity entity - Represents activities/services offered by providers
/// Responsible for: Activity details, pricing, capacity, availability, scheduling
/// </summary>
public class Activity : AuditableEntity
{
    private Activity() { } // Private constructor for EF Core

    // Provider relationship
    public Guid ProviderId { get; private set; }
    public virtual ActivityProvider Provider { get; private set; } = null!;

    // Category and Location
    public Guid CategoryId { get; private set; }
    public virtual Category Category { get; private set; } = null!;

    public Guid LocationId { get; private set; }
    public virtual Location Location { get; private set; } = null!;

    // Basic information
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ShortDescription { get; private set; }
    public string? CoverImageUrl { get; private set; }

    // Pricing
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "INR";
    public decimal? DiscountedPrice { get; private set; }
    public DateTime? DiscountValidUntil { get; private set; }

    // Capacity and Duration
    public int MinParticipants { get; private set; } = 1;
    public int MaxParticipants { get; private set; }
    public int DurationMinutes { get; private set; }
    public int? PreparationTimeMinutes { get; private set; }
    public int? CleanupTimeMinutes { get; private set; }

    // Scheduling
    public bool IsScheduled { get; private set; } = true;
    public int? AdvanceBookingDays { get; private set; }
    public int? CancellationHours { get; private set; }

    // Status and availability
    public ActivityStatus Status { get; private set; } = ActivityStatus.Draft;
    public bool IsActive { get; private set; } = false;
    public bool IsFeatured { get; private set; } = false;
    public DateTime? PublishedAt { get; private set; }

    // Requirements and policies
    public int? MinAge { get; private set; }
    public int? MaxAge { get; private set; }
    public string? DifficultyLevel { get; private set; } // 'beginner', 'intermediate', 'advanced'
    public string? AgeRequirement { get; private set; }
    public string? SkillLevel { get; private set; }
    public string? RequiredEquipment { get; private set; }
    public string? ProvidedEquipment { get; private set; }
    public string? WhatToBring { get; private set; }
    public string? MeetingPoint { get; private set; }
    public string? SafetyInstructions { get; private set; }
    public string? CancellationPolicy { get; private set; }
    public string? RefundPolicy { get; private set; }

    // Statistics
    public decimal AverageRating { get; private set; } = 0;
    public int TotalReviews { get; private set; } = 0;
    public int TotalBookings { get; private set; } = 0;
    public int ViewCount { get; private set; } = 0;

    // Navigation properties
    public virtual ICollection<Booking> Bookings { get; private set; } = new List<Booking>();
    public virtual ICollection<ActivityImage> Images { get; private set; } = new List<ActivityImage>();
    public virtual ICollection<ActivitySchedule> Schedules { get; private set; } = new List<ActivitySchedule>();
    public virtual ICollection<ActivityTag> Tags { get; private set; } = new List<ActivityTag>();
    public virtual ICollection<Review> Reviews { get; private set; } = new List<Review>();
    public virtual ICollection<Wishlist> Wishlists { get; private set; } = new List<Wishlist>();

    // Computed properties
    public decimal EffectivePrice
    {
        get
        {
            if (DiscountedPrice.HasValue &&
                DiscountValidUntil.HasValue &&
                DiscountValidUntil.Value > DateTime.UtcNow)
            {
                return DiscountedPrice.Value;
            }
            return Price;
        }
    }

    public bool HasActiveDiscount
    {
        get
        {
            return DiscountedPrice.HasValue &&
                   DiscountValidUntil.HasValue &&
                   DiscountValidUntil.Value > DateTime.UtcNow &&
                   DiscountedPrice.Value < Price;
        }
    }

    /// <summary>
    /// Factory method to create a new activity
    /// </summary>
    public static Activity Create(
        Guid providerId,
        Guid categoryId,
        Guid locationId,
        string title,
        string slug,
        string description,
        decimal price,
        int maxParticipants,
        int durationMinutes,
        string currency = "INR")
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("Provider ID is required", nameof(providerId));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category ID is required", nameof(categoryId));

        if (locationId == Guid.Empty)
            throw new ArgumentException("Location ID is required", nameof(locationId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug is required", nameof(slug));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        if (maxParticipants <= 0)
            throw new ArgumentException("Max participants must be greater than 0", nameof(maxParticipants));

        if (durationMinutes <= 0)
            throw new ArgumentException("Duration must be greater than 0", nameof(durationMinutes));

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            CategoryId = categoryId,
            LocationId = locationId,
            Title = title.Trim(),
            Slug = slug.ToLowerInvariant().Trim(),
            Description = description.Trim(),
            Price = price,
            Currency = currency.ToUpperInvariant(),
            MinParticipants = 1,
            MaxParticipants = maxParticipants,
            DurationMinutes = durationMinutes,
            Status = ActivityStatus.Draft,
            IsActive = false,
            IsFeatured = false,
            AverageRating = 0,
            TotalReviews = 0,
            TotalBookings = 0,
            ViewCount = 0
        };

        return activity;
    }

    /// <summary>
    /// Update basic information
    /// </summary>
    public void UpdateBasicInfo(
        string title,
        string description,
        string? shortDescription,
        string? coverImageUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        Title = title.Trim();
        Description = description.Trim();
        ShortDescription = shortDescription?.Trim();
        CoverImageUrl = coverImageUrl?.Trim();
    }

    /// <summary>
    /// Update pricing
    /// </summary>
    public void UpdatePricing(decimal price, string currency)
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        Price = price;
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Set discount
    /// </summary>
    public void SetDiscount(decimal discountedPrice, DateTime validUntil)
    {
        if (discountedPrice < 0)
            throw new ArgumentException("Discounted price cannot be negative", nameof(discountedPrice));

        if (discountedPrice >= Price)
            throw new ArgumentException("Discounted price must be less than regular price", nameof(discountedPrice));

        if (validUntil <= DateTime.UtcNow)
            throw new ArgumentException("Valid until must be in the future", nameof(validUntil));

        DiscountedPrice = discountedPrice;
        DiscountValidUntil = validUntil;
    }

    /// <summary>
    /// Clear discount
    /// </summary>
    public void ClearDiscount()
    {
        DiscountedPrice = null;
        DiscountValidUntil = null;
    }

    /// <summary>
    /// Update capacity
    /// </summary>
    public void UpdateCapacity(int minParticipants, int maxParticipants)
    {
        if (minParticipants < 1)
            throw new ArgumentException("Min participants must be at least 1", nameof(minParticipants));

        if (maxParticipants < minParticipants)
            throw new ArgumentException("Max participants must be greater than or equal to min participants", nameof(maxParticipants));

        MinParticipants = minParticipants;
        MaxParticipants = maxParticipants;
    }

    /// <summary>
    /// Update duration
    /// </summary>
    public void UpdateDuration(int durationMinutes, int? preparationTimeMinutes, int? cleanupTimeMinutes)
    {
        if (durationMinutes <= 0)
            throw new ArgumentException("Duration must be greater than 0", nameof(durationMinutes));

        DurationMinutes = durationMinutes;
        PreparationTimeMinutes = preparationTimeMinutes;
        CleanupTimeMinutes = cleanupTimeMinutes;
    }

    /// <summary>
    /// Update scheduling settings
    /// </summary>
    public void UpdateScheduling(
        bool isScheduled,
        int? advanceBookingDays,
        int? cancellationHours)
    {
        IsScheduled = isScheduled;
        AdvanceBookingDays = advanceBookingDays;
        CancellationHours = cancellationHours;
    }

    /// <summary>
    /// Update age requirements
    /// </summary>
    public void UpdateAgeRequirements(int? minAge, int? maxAge)
    {
        if (minAge.HasValue && minAge.Value < 0)
            throw new ArgumentException("Min age cannot be negative", nameof(minAge));

        if (maxAge.HasValue && minAge.HasValue && maxAge.Value < minAge.Value)
            throw new ArgumentException("Max age must be greater than or equal to min age", nameof(maxAge));

        MinAge = minAge;
        MaxAge = maxAge;
    }

    /// <summary>
    /// Update policies and requirements
    /// </summary>
    public void UpdatePolicies(
        string? cancellationPolicy,
        string? refundPolicy,
        string? safetyInstructions,
        string? whatToBring = null,
        string? meetingPoint = null,
        string? difficultyLevel = null,
        string? ageRequirement = null)
    {
        CancellationPolicy = cancellationPolicy?.Trim();
        RefundPolicy = refundPolicy?.Trim();
        SafetyInstructions = safetyInstructions?.Trim();
        WhatToBring = whatToBring?.Trim();
        MeetingPoint = meetingPoint?.Trim();
        DifficultyLevel = difficultyLevel?.Trim();
        AgeRequirement = ageRequirement?.Trim();
    }

    /// <summary>
    /// Publish activity
    /// </summary>
    public void Publish()
    {
        if (Status == ActivityStatus.Archived)
            throw new InvalidOperationException("Cannot publish archived activity");

        Status = ActivityStatus.Active;
        IsActive = true;
        PublishedAt = PublishedAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Unpublish activity
    /// </summary>
    public void Unpublish()
    {
        Status = ActivityStatus.Inactive;
        IsActive = false;
    }

    /// <summary>
    /// Archive activity
    /// </summary>
    public void Archive()
    {
        Status = ActivityStatus.Archived;
        IsActive = false;
    }

    /// <summary>
    /// Feature activity
    /// </summary>
    public void Feature()
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot feature inactive activity");

        IsFeatured = true;
    }

    /// <summary>
    /// Unfeature activity
    /// </summary>
    public void Unfeature()
    {
        IsFeatured = false;
    }

    /// <summary>
    /// Increment view count
    /// </summary>
    public void IncrementViewCount()
    {
        ViewCount++;
    }

    /// <summary>
    /// Update rating statistics
    /// </summary>
    public void UpdateRating(decimal averageRating, int totalReviews)
    {
        if (averageRating < 0 || averageRating > 5)
            throw new ArgumentException("Average rating must be between 0 and 5", nameof(averageRating));

        if (totalReviews < 0)
            throw new ArgumentException("Total reviews cannot be negative", nameof(totalReviews));

        AverageRating = averageRating;
        TotalReviews = totalReviews;
    }

    /// <summary>
    /// Increment booking count
    /// </summary>
    public void IncrementBookingCount()
    {
        TotalBookings++;
    }

    /// <summary>
    /// Check if activity is bookable
    /// </summary>
    public bool IsBookable()
    {
        return IsActive && Status == ActivityStatus.Active;
    }

    /// <summary>
    /// Check if participant count is valid
    /// </summary>
    public bool IsValidParticipantCount(int count)
    {
        return count >= MinParticipants && count <= MaxParticipants;
    }

    /// <summary>
    /// Calculate total duration including prep and cleanup
    /// </summary>
    public int GetTotalDurationMinutes()
    {
        return DurationMinutes +
               (PreparationTimeMinutes ?? 0) +
               (CleanupTimeMinutes ?? 0);
    }
}
