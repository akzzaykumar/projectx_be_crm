using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Reviews.Commands.MarkReviewHelpful;

/// <summary>
/// Command to mark a review as helpful
/// </summary>
public record MarkReviewHelpfulCommand : IRequest<Result<bool>>
{
    public Guid ReviewId { get; init; }
}
