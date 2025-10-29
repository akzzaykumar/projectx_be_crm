using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Bookings.Commands.CompleteBooking;

public record CompleteBookingCommand(Guid BookingId) : IRequest<Result<Unit>>;
