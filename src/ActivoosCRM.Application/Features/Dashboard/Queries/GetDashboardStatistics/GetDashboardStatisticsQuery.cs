using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Dashboard.Queries.GetDashboardStatistics;

/// <summary>
/// Query to get dashboard statistics for current user (role-based)
/// </summary>
public class GetDashboardStatisticsQuery : IRequest<Result<object>>
{
    // Uses current user and their role from ICurrentUserService
}

/// <summary>
/// Handler for GetDashboardStatisticsQuery
/// Returns different dashboard based on user role (Customer vs Provider)
/// </summary>
public class GetDashboardStatisticsQueryHandler : IRequestHandler<GetDashboardStatisticsQuery, Result<object>>
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetDashboardStatisticsQueryHandler> _logger;

    public GetDashboardStatisticsQueryHandler(
        IAnalyticsService analyticsService,
        ICurrentUserService currentUserService,
        IApplicationDbContext context,
        ILogger<GetDashboardStatisticsQueryHandler> logger)
    {
        _analyticsService = analyticsService;
        _currentUserService = currentUserService;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<object>> Handle(
        GetDashboardStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Result<object>.CreateFailure("User not authenticated");
            }

            _logger.LogInformation("Getting dashboard statistics for user {UserId}", userId.Value);

            // Get user to determine role
            var user = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
            if (user == null)
            {
                return Result<object>.CreateFailure("User not found");
            }

            // Return appropriate dashboard based on role  
            if (user.Role == Domain.Enums.UserRole.ActivityProvider)
            {
                var providerDashboard = await _analyticsService.GetProviderDashboardAsync(
                    userId.Value,
                    cancellationToken);

                return Result<object>.CreateSuccess(providerDashboard);
            }
            else
            {
                var customerDashboard = await _analyticsService.GetCustomerDashboardAsync(
                    userId.Value,
                    cancellationToken);

                return Result<object>.CreateSuccess(customerDashboard);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard statistics");
            return Result<object>.CreateFailure("Failed to retrieve dashboard statistics");
        }
    }
}