using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Activities.Queries.GetActivityById;

/// <summary>
/// Handler for GetActivityByIdQuery
/// </summary>
public class GetActivityByIdQueryHandler : IRequestHandler<GetActivityByIdQuery, Result<ActivityDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetActivityByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ActivityDetailDto>> Handle(
        GetActivityByIdQuery request,
        CancellationToken cancellationToken)
    {
        var activity = await _context.Activities
            .Include(a => a.Category)
            .Include(a => a.Location)
            .Include(a => a.Provider)
            .Include(a => a.Images)
            .Include(a => a.Schedules)
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

        if (activity == null)
        {
            return Result<ActivityDetailDto>.CreateFailure("Activity not found");
        }

        var activityDetailDto = new ActivityDetailDto
        {
            ActivityId = activity.Id,
            Title = activity.Title,
            Slug = activity.Slug,
            Description = activity.Description,
            ShortDescription = activity.ShortDescription,
            CoverImageUrl = activity.CoverImageUrl,

            // Pricing
            Price = activity.Price,
            DiscountedPrice = activity.DiscountedPrice,
            Currency = activity.Currency,
            HasActiveDiscount = activity.HasActiveDiscount,
            DiscountValidUntil = activity.DiscountValidUntil,

            // Capacity and Duration
            MinParticipants = activity.MinParticipants,
            MaxParticipants = activity.MaxParticipants,
            DurationMinutes = activity.DurationMinutes,

            // Requirements
            MinAge = activity.MinAge,
            MaxAge = activity.MaxAge,
            DifficultyLevel = activity.DifficultyLevel,
            AgeRequirement = activity.AgeRequirement,
            SkillLevel = activity.SkillLevel,
            RequiredEquipment = activity.RequiredEquipment,
            ProvidedEquipment = activity.ProvidedEquipment,
            WhatToBring = activity.WhatToBring,
            MeetingPoint = activity.MeetingPoint,
            SafetyInstructions = activity.SafetyInstructions,

            // Policies
            CancellationPolicy = activity.CancellationPolicy,
            RefundPolicy = activity.RefundPolicy,

            // Statistics
            AverageRating = activity.AverageRating,
            TotalReviews = activity.TotalReviews,
            TotalBookings = activity.TotalBookings,
            ViewCount = activity.ViewCount,

            // Category
            Category = new CategoryDetailDto
            {
                CategoryId = activity.Category.Id,
                Name = activity.Category.Name
            },

            // Location
            Location = new LocationDetailDto
            {
                LocationId = activity.Location.Id,
                Name = activity.Location.Name,
                City = activity.Location.City,
                State = activity.Location.State
            },

            // Provider
            Provider = new ProviderDetailDto
            {
                ProviderId = activity.Provider.Id,
                BusinessName = activity.Provider.BusinessName,
                Description = activity.Provider.Description,
                AverageRating = activity.Provider.AverageRating,
                TotalReviews = activity.Provider.TotalReviews,
                IsVerified = activity.Provider.IsVerified,
                BusinessPhone = activity.Provider.BusinessPhone,
                BusinessEmail = activity.Provider.BusinessEmail
            },

            // Images
            Images = activity.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new ActivityImageDto
                {
                    ImageId = i.Id,
                    ImageUrl = i.ImageUrl,
                    Caption = i.Caption,
                    IsPrimary = i.IsPrimary,
                    SortOrder = i.SortOrder
                })
                .ToList(),

            // Schedules
            Schedules = activity.Schedules
                .Where(s => s.IsActive)
                .Select(s => new ActivityScheduleDto
                {
                    ScheduleId = s.Id,
                    StartTime = TimeSpan.FromHours(s.StartTime.Hour).Add(TimeSpan.FromMinutes(s.StartTime.Minute)),
                    EndTime = TimeSpan.FromHours(s.EndTime.Hour).Add(TimeSpan.FromMinutes(s.EndTime.Minute)),
                    DaysOfWeek = s.DaysOfWeek.Select(d => GetDayName(d)).ToList(),
                    AvailableSpots = s.AvailableSpots,
                    IsActive = s.IsActive
                })
                .ToList(),

            // Tags
            Tags = activity.Tags.Select(t => t.Tag).ToList(),

            // Status
            IsFeatured = activity.IsFeatured,
            IsActive = activity.IsActive,
            Status = activity.Status.ToString(),
            PublishedAt = activity.PublishedAt,
            CreatedAt = activity.CreatedAt
        };

        // Increment view count asynchronously (fire and forget for performance)
        _ = Task.Run(async () =>
        {
            try
            {
                activity.IncrementViewCount();
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Silently fail - view count increment is not critical
            }
        }, cancellationToken);

        return Result<ActivityDetailDto>.CreateSuccess(activityDetailDto);
    }

    private static string GetDayName(int dayNumber)
    {
        return dayNumber switch
        {
            0 => "Sunday",
            1 => "Monday",
            2 => "Tuesday",
            3 => "Wednesday",
            4 => "Thursday",
            5 => "Friday",
            6 => "Saturday",
            _ => "Unknown"
        };
    }
}
