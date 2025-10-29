using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.LocationRequests.Commands.ApproveLocationRequest;

public record ApproveLocationRequestCommand(Guid LocationRequestId) : IRequest<Result<Unit>>;
