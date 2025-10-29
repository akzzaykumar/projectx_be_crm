using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.LocationRequests.Commands.RequestLocation;

public record RequestLocationCommand(
    string Name,
    string City,
    string State,
    string Country,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    string? Reason
) : IRequest<Result<RequestLocationResponse>>;

public record RequestLocationResponse
{
    public Guid LocationRequestId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
