using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Bookings.Commands.CreateBooking;

/// <summary>
/// Command to create a new booking
/// </summary>
public record CreateBookingCommand : IRequest<Result<CreateBookingResponse>>
{
    public Guid ActivityId { get; init; }
    public DateTime BookingDate { get; init; }
    public TimeSpan BookingTime { get; init; }
    public int NumberOfParticipants { get; init; }
    public string? SpecialRequests { get; init; }
    public string? ParticipantNames { get; init; }
    public string? CustomerNotes { get; init; }
    public List<ParticipantDto>? Participants { get; init; }
    public string? CouponCode { get; init; }
}

public record ParticipantDto
{
    public string Name { get; init; } = string.Empty;
    public int? Age { get; init; }
    public string? Gender { get; init; }
    public string? ContactPhone { get; init; }
}

public record CreateBookingResponse
{
    public Guid BookingId { get; init; }
    public string BookingReference { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public bool PaymentRequired { get; init; }
}
