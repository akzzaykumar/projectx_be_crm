using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Activities.Queries.GetActivityById;

/// <summary>
/// Query to get detailed activity information by ID
/// </summary>
public record GetActivityByIdQuery(Guid ActivityId) : IRequest<Result<ActivityDetailDto>>;

/// <summary>
/// Detailed activity DTO with all information
/// </summary>
public record ActivityDetailDto
{
    public Guid ActivityId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? CoverImageUrl { get; init; }

    // Pricing
    public decimal Price { get; init; }
    public decimal? DiscountedPrice { get; init; }
    public string Currency { get; init; } = "INR";
    public bool HasActiveDiscount { get; init; }
    public DateTime? DiscountValidUntil { get; init; }

    // Capacity and Duration
    public int MinParticipants { get; init; }
    public int MaxParticipants { get; init; }
    public int DurationMinutes { get; init; }

    // Requirements
    public int? MinAge { get; init; }
    public int? MaxAge { get; init; }
    public string? DifficultyLevel { get; init; }
    public string? AgeRequirement { get; init; }
    public string? SkillLevel { get; init; }
    public string? RequiredEquipment { get; init; }
    public string? ProvidedEquipment { get; init; }
    public string? WhatToBring { get; init; }
    public string? MeetingPoint { get; init; }
    public string? SafetyInstructions { get; init; }

    // Policies
    public string? CancellationPolicy { get; init; }
    public string? RefundPolicy { get; init; }

    // Statistics
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public int TotalBookings { get; init; }
    public int ViewCount { get; init; }

    // Related entities
    public CategoryDetailDto Category { get; init; } = null!;
    public LocationDetailDto Location { get; init; } = null!;
    public ProviderDetailDto Provider { get; init; } = null!;

    // Collections
    public List<ActivityImageDto> Images { get; init; } = new();
    public List<ActivityScheduleDto> Schedules { get; init; } = new();
    public List<string> Tags { get; init; } = new();

    // Status
    public bool IsFeatured { get; init; }
    public bool IsActive { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? PublishedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CategoryDetailDto
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record LocationDetailDto
{
    public Guid LocationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
}

public record ProviderDetailDto
{
    public Guid ProviderId { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public bool IsVerified { get; init; }
    public string? BusinessPhone { get; init; }
    public string? BusinessEmail { get; init; }
}

public record ActivityImageDto
{
    public Guid ImageId { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public string? Caption { get; init; }
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
}

public record ActivityScheduleDto
{
    public Guid ScheduleId { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public List<string> DaysOfWeek { get; init; } = new();
    public int AvailableSpots { get; init; }
    public bool IsActive { get; init; }
}
