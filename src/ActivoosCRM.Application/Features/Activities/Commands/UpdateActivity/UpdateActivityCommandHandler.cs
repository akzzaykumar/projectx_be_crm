using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Activities.Commands.UpdateActivity;

/// <summary>
/// Handler for UpdateActivityCommand
/// </summary>
public class UpdateActivityCommandHandler : IRequestHandler<UpdateActivityCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateActivityCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(
        UpdateActivityCommand request,
        CancellationToken cancellationToken)
    {
        // Get current user
        var userId = _currentUserService.GetCurrentUserId();
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return Result<bool>.CreateFailure("User not authenticated");
        }

        // Get activity with provider
        var activity = await _context.Activities
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

        if (activity == null)
        {
            return Result<bool>.CreateFailure("Activity not found");
        }

        // Verify ownership
        if (activity.Provider.UserId != userId.Value)
        {
            return Result<bool>.CreateFailure("You don't have permission to update this activity");
        }

        // Update basic info
        activity.UpdateBasicInfo(
            request.Title,
            request.Description,
            request.ShortDescription,
            request.CoverImageUrl);

        // Update pricing
        activity.UpdatePricing(request.Price, request.Currency);

        // Update capacity
        activity.UpdateCapacity(request.MinParticipants, request.MaxParticipants);

        // Update duration
        activity.UpdateDuration(request.DurationMinutes, null, null);

        // Update age requirements
        if (request.MinAge.HasValue || request.MaxAge.HasValue)
        {
            activity.UpdateAgeRequirements(request.MinAge, request.MaxAge);
        }

        // Update policies
        activity.UpdatePolicies(
            cancellationPolicy: request.CancellationPolicy,
            refundPolicy: request.RefundPolicy,
            safetyInstructions: request.SafetyInstructions,
            whatToBring: request.WhatToBring,
            meetingPoint: request.MeetingPoint,
            difficultyLevel: request.DifficultyLevel);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.CreateSuccess(true);
    }
}
