using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Activities.Commands.CreateActivity;

/// <summary>
/// Handler for CreateActivityCommand
/// </summary>
public class CreateActivityCommandHandler : IRequestHandler<CreateActivityCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateActivityCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(
        CreateActivityCommand request,
        CancellationToken cancellationToken)
    {
        // Get current user ID
        var userId = _currentUserService.GetCurrentUserId();
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return Result<Guid>.CreateFailure("User not authenticated");
        }

        // Verify user has a provider profile
        var provider = await _context.ActivityProviders
            .FirstOrDefaultAsync(p => p.UserId == userId.Value, cancellationToken);

        if (provider == null)
        {
            return Result<Guid>.CreateFailure("Provider profile not found. You must be a registered provider to create activities.");
        }

        // Validate category exists
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken);

        if (!categoryExists)
        {
            return Result<Guid>.CreateFailure("Category not found or is inactive");
        }

        // Validate location exists
        var locationExists = await _context.Locations
            .AnyAsync(l => l.Id == request.LocationId, cancellationToken);

        if (!locationExists)
        {
            return Result<Guid>.CreateFailure("Location not found");
        }

        // Check slug uniqueness
        var slugExists = await _context.Activities
            .AnyAsync(a => a.Slug.ToLower() == request.Slug.ToLower(), cancellationToken);

        if (slugExists)
        {
            return Result<Guid>.CreateFailure("An activity with this slug already exists. Please choose a different slug.");
        }

        // Create activity
        var activity = Activity.Create(
            providerId: provider.Id,
            categoryId: request.CategoryId,
            locationId: request.LocationId,
            title: request.Title,
            slug: request.Slug,
            description: request.Description,
            price: request.Price,
            maxParticipants: request.MaxParticipants,
            durationMinutes: request.DurationMinutes,
            currency: request.Currency);

        // Update optional fields
        if (!string.IsNullOrWhiteSpace(request.ShortDescription))
        {
            activity.UpdateBasicInfo(
                request.Title,
                request.Description,
                request.ShortDescription,
                null);
        }

        if (request.MinAge.HasValue || request.MaxAge.HasValue)
        {
            activity.UpdateAgeRequirements(request.MinAge, request.MaxAge);
        }

        if (!string.IsNullOrWhiteSpace(request.CancellationPolicy) ||
            !string.IsNullOrWhiteSpace(request.WhatToBring) ||
            !string.IsNullOrWhiteSpace(request.MeetingPoint) ||
            !string.IsNullOrWhiteSpace(request.DifficultyLevel))
        {
            activity.UpdatePolicies(
                cancellationPolicy: request.CancellationPolicy,
                refundPolicy: null,
                safetyInstructions: null,
                whatToBring: request.WhatToBring,
                meetingPoint: request.MeetingPoint,
                difficultyLevel: request.DifficultyLevel);
        }

        // Add activity to context
        _context.Activities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.CreateSuccess(activity.Id);
    }
}
