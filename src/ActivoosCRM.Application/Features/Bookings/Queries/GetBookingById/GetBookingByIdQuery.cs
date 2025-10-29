using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Bookings.Queries.GetBookingById;

public record GetBookingByIdQuery(Guid BookingId) : IRequest<Result<BookingDetailDto>>;

public record BookingDetailDto
{
    public Guid Id { get; init; }
    public string BookingReference { get; init; } = string.Empty;
    public DateTime BookingDate { get; init; }
    public TimeSpan BookingTime { get; init; }
    public int NumberOfParticipants { get; init; }
    public string Status { get; init; } = string.Empty;

    // Pricing breakdown
    public decimal PricePerParticipant { get; init; }
    public decimal SubtotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "INR";

    // Optional information
    public string? CouponCode { get; init; }
    public decimal? CouponDiscountPercentage { get; init; }
    public string? SpecialRequests { get; init; }
    public string? ParticipantNames { get; init; }
    public string? CustomerNotes { get; init; }
    public string? CancellationReason { get; init; }
    public decimal RefundAmount { get; init; }

    // Activity details
    public ActivityInfo Activity { get; init; } = null!;

    // Customer details
    public CustomerInfo Customer { get; init; } = null!;

    // Payment details
    public PaymentInfo? Payment { get; init; }

    // Participants list
    public List<ParticipantInfo> Participants { get; init; } = new();

    // Computed flags
    public bool CanBeCancelled { get; init; }
    public bool IsPaid { get; init; }
    public bool IsUpcoming { get; init; }

    // Timestamps
    public DateTime CreatedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? CheckedInAt { get; init; }
}
public record ActivityInfo
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? CoverImageUrl { get; init; }
    public int DurationMinutes { get; init; }
    public string? MeetingPoint { get; init; }
    public string? WhatToBring { get; init; }

    public LocationInfo Location { get; init; } = null!;
    public ProviderInfo Provider { get; init; } = null!;
}

public record LocationInfo
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
}

public record ProviderInfo
{
    public Guid Id { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
}

public record CustomerInfo
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public record PaymentInfo
{
    public Guid Id { get; init; }
    public string PaymentReference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string Status { get; init; } = string.Empty;
    public string? PaymentGateway { get; init; }
    public string? PaymentMethod { get; init; }
    public string? CardLast4Digits { get; init; }
    public string? CardBrand { get; init; }
    public decimal RefundedAmount { get; init; }
    public DateTime? PaidAt { get; init; }
}

public record ParticipantInfo
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public string? Gender { get; init; }
    public string? ContactPhone { get; init; }
    public string? DietaryRestrictions { get; init; }
    public string? MedicalConditions { get; init; }
}
