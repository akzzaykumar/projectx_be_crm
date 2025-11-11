using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Dashboard.Queries.GetRevenueAnalytics;

/// <summary>
/// Query to get revenue analytics for provider
/// </summary>
public class GetRevenueAnalyticsQuery : IRequest<Result<RevenueAnalyticsDto>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string Period { get; set; } = "monthly"; // daily, weekly, monthly, yearly
}

/// <summary>
/// Validator for GetRevenueAnalyticsQuery
/// </summary>
public class GetRevenueAnalyticsQueryValidator : AbstractValidator<GetRevenueAnalyticsQuery>
{
    public GetRevenueAnalyticsQueryValidator()
    {
        RuleFor(x => x.Period)
            .NotEmpty()
            .WithMessage("Period is required")
            .Must(p => new[] { "daily", "weekly", "monthly", "yearly" }.Contains(p.ToLower()))
            .WithMessage("Period must be one of: daily, weekly, monthly, yearly");

        RuleFor(x => x.FromDate)
            .LessThan(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("From date must be before to date");
    }
}

/// <summary>
/// Handler for GetRevenueAnalyticsQuery
/// </summary>
public class GetRevenueAnalyticsQueryHandler : IRequestHandler<GetRevenueAnalyticsQuery, Result<RevenueAnalyticsDto>>
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetRevenueAnalyticsQueryHandler> _logger;

    public GetRevenueAnalyticsQueryHandler(
        IAnalyticsService analyticsService,
        ICurrentUserService currentUserService,
        IApplicationDbContext context,
        ILogger<GetRevenueAnalyticsQueryHandler> logger)
    {
        _analyticsService = analyticsService;
        _currentUserService = currentUserService;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<RevenueAnalyticsDto>> Handle(
        GetRevenueAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Result<RevenueAnalyticsDto>.CreateFailure("User not authenticated");
            }

            // Get provider profile
            var provider = await _context.ActivityProviders
                .FirstOrDefaultAsync(p => p.UserId == userId.Value && !p.IsDeleted, cancellationToken);

            if (provider == null)
            {
                return Result<RevenueAnalyticsDto>.CreateFailure("Provider profile not found");
            }

            _logger.LogInformation(
                "Getting revenue analytics for provider {ProviderId}, period: {Period}",
                provider.Id, request.Period);

            // Set default date range if not provided
            var toDate = request.ToDate ?? DateTime.UtcNow;
            var fromDate = request.FromDate ?? toDate.AddMonths(-12);

            var analytics = await _analyticsService.GetRevenueAnalyticsAsync(
                provider.Id,
                fromDate,
                toDate,
                request.Period,
                cancellationToken);

            return Result<RevenueAnalyticsDto>.CreateSuccess(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue analytics");
            return Result<RevenueAnalyticsDto>.CreateFailure("Failed to retrieve revenue analytics");
        }
    }
}