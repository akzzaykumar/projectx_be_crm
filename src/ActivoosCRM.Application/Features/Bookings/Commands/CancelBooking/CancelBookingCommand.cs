using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Bookings.Commands.CancelBooking;

public record CancelBookingCommand(
    Guid BookingId,
    string CancellationReason
) : IRequest<Result<CancelBookingResponse>>;

public record CancelBookingResponse
{
    public Guid BookingId { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal RefundAmount { get; init; }
    public string RefundStatus { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
