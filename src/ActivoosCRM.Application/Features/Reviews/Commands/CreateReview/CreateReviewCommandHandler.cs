using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using ActivoosCRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Reviews.Commands.CreateReview;

/// <summary>
/// Handler for CreateReviewCommand
/// Creates a review for a completed booking and updates activity/provider ratings
/// </summary>
public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<CreateReviewResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly ILogger<CreateReviewCommandHandler> _logger;

    public CreateReviewCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILoyaltyService loyaltyService,
        ILogger<CreateReviewCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _loyaltyService = loyaltyService;
        _logger = logger;
    }

    public async Task<Result<CreateReviewResponse>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            if (currentUserId == null || currentUserId == Guid.Empty)
                return Result<CreateReviewResponse>.CreateFailure("User not authenticated");

            // Get booking with related entities
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Activity)
                    .ThenInclude(a => a.Provider)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                return Result<CreateReviewResponse>.CreateFailure("Booking not found");

            // Verify booking belongs to current user
            if (booking.Customer.UserId != currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to review booking {BookingId} that doesn't belong to them",
                    currentUserId, request.BookingId);
                return Result<CreateReviewResponse>.CreateFailure("You can only review your own bookings");
            }

            // Check booking is completed
            if (booking.Status != BookingStatus.Completed)
                return Result<CreateReviewResponse>.CreateFailure("You can only review completed bookings");

            // Check if user has already reviewed this booking
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BookingId == request.BookingId && !r.IsDeleted, cancellationToken);

            if (existingReview != null)
                return Result<CreateReviewResponse>.CreateFailure("You have already reviewed this booking");

            // Create review
            var review = Review.Create(
                bookingId: booking.Id,
                customerId: booking.CustomerId,
                activityId: booking.ActivityId,
                providerId: booking.Activity.ProviderId,
                rating: request.Rating,
                title: request.Title,
                reviewText: request.ReviewText
            );

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync(cancellationToken);

            // Update activity and provider ratings
            await UpdateActivityRatings(booking.ActivityId, cancellationToken);
            await UpdateProviderRatings(booking.Activity.ProviderId, cancellationToken);

            // Award loyalty points for review
            try
            {
                await _loyaltyService.AwardReviewPointsAsync(
                    review.Id,
                    cancellationToken);

                _logger.LogInformation(
                    "Loyalty points awarded for review {ReviewId}",
                    review.Id);
            }
            catch (Exception loyaltyEx)
            {
                _logger.LogError(loyaltyEx,
                    "Failed to award loyalty points for review {ReviewId}. Review creation succeeded.",
                    review.Id);
                // Don't fail review creation if loyalty points award fails
            }

            _logger.LogInformation("Review {ReviewId} created for booking {BookingId} by user {UserId}",
                review.Id, booking.Id, currentUserId);

            return Result<CreateReviewResponse>.CreateSuccess(new CreateReviewResponse
            {
                ReviewId = review.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review for booking {BookingId}", request.BookingId);
            return Result<CreateReviewResponse>.CreateFailure("Failed to create review. Please try again.");
        }
    }

    /// <summary>
    /// Update activity average rating and total reviews count
    /// </summary>
    private async Task UpdateActivityRatings(Guid activityId, CancellationToken cancellationToken)
    {
        var activityReviews = await _context.Reviews
            .Where(r => r.ActivityId == activityId && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        var activity = await _context.Activities.FindAsync(new object[] { activityId }, cancellationToken);
        if (activity != null)
        {
            var averageRating = activityReviews.Any()
                ? (decimal)activityReviews.Average(r => r.Rating)
                : 0m;

            activity.UpdateRating(Math.Round(averageRating, 1), activityReviews.Count);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated activity {ActivityId} rating to {Rating} ({ReviewCount} reviews)",
                activityId, averageRating, activityReviews.Count);
        }
    }

    /// <summary>
    /// Update provider average rating and total reviews count
    /// </summary>
    private async Task UpdateProviderRatings(Guid providerId, CancellationToken cancellationToken)
    {
        var providerReviews = await _context.Reviews
            .Where(r => r.ProviderId == providerId && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        var provider = await _context.ActivityProviders.FindAsync(new object[] { providerId }, cancellationToken);
        if (provider != null)
        {
            var averageRating = providerReviews.Any()
                ? (decimal)providerReviews.Average(r => r.Rating)
                : 0m;

            provider.UpdateRating(Math.Round(averageRating, 1), providerReviews.Count);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated provider {ProviderId} rating to {Rating} ({ReviewCount} reviews)",
                providerId, averageRating, providerReviews.Count);
        }
    }
}
