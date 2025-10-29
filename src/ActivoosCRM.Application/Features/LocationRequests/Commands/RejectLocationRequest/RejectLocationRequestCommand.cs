using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.LocationRequests.Commands.RejectLocationRequest;

public record RejectLocationRequestCommand(
    Guid LocationRequestId,
    string RejectionReason
) : IRequest<Result<Unit>>;
