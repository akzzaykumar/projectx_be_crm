using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Reviews.Commands.MarkReviewHelpful;

/// <summary>
/// Handler for MarkReviewHelpfulCommand
/// Increments the helpful count for a review
/// </summary>
public class MarkReviewHelpfulCommandHandler : IRequestHandler<MarkReviewHelpfulCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MarkReviewHelpfulCommandHandler> _logger;

    public MarkReviewHelpfulCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<MarkReviewHelpfulCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(MarkReviewHelpfulCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (currentUserId == null || currentUserId == Guid.Empty)
                return Result<bool>.CreateFailure("User not authenticated");

            // Get review
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == request.ReviewId && !r.IsDeleted, cancellationToken);

            if (review == null)
                return Result<bool>.CreateFailure("Review not found");

            // Increment helpful count
            review.IncrementHelpfulCount();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} marked review {ReviewId} as helpful (count: {HelpfulCount})",
                currentUserId, review.Id, review.HelpfulCount);

            return Result<bool>.CreateSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking review {ReviewId} as helpful", request.ReviewId);
            return Result<bool>.CreateFailure("Failed to mark review as helpful. Please try again.");
        }
    }
}
