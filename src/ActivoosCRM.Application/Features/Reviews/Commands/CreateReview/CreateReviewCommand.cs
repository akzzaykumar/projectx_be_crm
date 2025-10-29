using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Reviews.Commands.CreateReview;

/// <summary>
/// Command to create a review for a completed booking
/// </summary>
public record CreateReviewCommand : IRequest<Result<CreateReviewResponse>>
{
    public Guid BookingId { get; init; }
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? ReviewText { get; init; }
}

/// <summary>
/// Response containing the created review ID
/// </summary>
public record CreateReviewResponse
{
    public Guid ReviewId { get; init; }
}
