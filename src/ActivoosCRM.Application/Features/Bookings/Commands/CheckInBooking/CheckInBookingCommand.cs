using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Bookings.Commands.CheckInBooking;

public record CheckInBookingCommand(Guid BookingId) : IRequest<Result<Unit>>;
