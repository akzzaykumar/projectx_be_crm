using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Bookings.Queries.GetBookings;

/// <summary>
/// Query to get user's bookings with filters and pagination
/// </summary>
public record GetBookingsQuery : IRequest<Result<PaginatedList<BookingDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public Guid? ActivityId { get; init; }
}

/// <summary>
/// Booking DTO for list view
/// </summary>
public record BookingDto
{
    public Guid BookingId { get; init; }
    public string BookingReference { get; init; } = string.Empty;
    public DateTime BookingDate { get; init; }
    public TimeSpan BookingTime { get; init; }
    public int NumberOfParticipants { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "INR";
    public string? SpecialRequests { get; init; }
    public ActivitySummary Activity { get; init; } = null!;
    public PaymentSummary? Payment { get; init; }
    public bool CanBeCancelled { get; init; }
    public bool IsPaid { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ActivitySummary
{
    public Guid ActivityId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? CoverImageUrl { get; init; }
    public int DurationMinutes { get; init; }
    public LocationSummary Location { get; init; } = null!;
    public ProviderSummary Provider { get; init; } = null!;
}

public record LocationSummary
{
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
}

public record ProviderSummary
{
    public string BusinessName { get; init; } = string.Empty;
    public string? BusinessPhone { get; init; }
}

public record PaymentSummary
{
    public Guid PaymentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
}
