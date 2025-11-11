using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Infrastructure.Services;

/// <summary>
/// Service for analytics and dashboard metrics calculation
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IApplicationDbContext context,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CustomerDashboardDto> GetCustomerDashboardAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(cp => cp.UserId == userId, cancellationToken);

            if (customerProfile == null)
                return new CustomerDashboardDto();

            var bookings = await _context.Bookings
                .Where(b => b.CustomerId == customerProfile.Id && !b.IsDeleted)
                .ToListAsync(cancellationToken);

            var favoriteActivities = await _context.Bookings
                .Where(b => b.CustomerId == customerProfile.Id && b.Status == BookingStatus.Completed && !b.IsDeleted)
                .GroupBy(b => new { b.ActivityId, b.Activity.Title, b.Activity.CoverImageUrl })
                .Select(g => new FavoriteActivityDto
                {
                    ActivityId = g.Key.ActivityId,
                    Title = g.Key.Title,
                    BookingCount = g.Count(),
                    CoverImageUrl = g.Key.CoverImageUrl
                })
                .OrderByDescending(f => f.BookingCount)
                .Take(5)
                .ToListAsync(cancellationToken);

            var recentBookings = await _context.Bookings
                .Where(b => b.CustomerId == customerProfile.Id && !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new RecentBookingDto
                {
                    BookingId = b.Id,
                    ActivityTitle = b.Activity.Title,
                    BookingDate = b.BookingDate,
                    Status = b.Status.ToString(),
                    Amount = b.TotalAmount
                })
                .ToListAsync(cancellationToken);

            // var wishlistCount = await _context.Wishlists
            //     // .CountAsync(w => w.Customer.UserId == userId, cancellationToken);

            var unreadNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

            return new CustomerDashboardDto
            {
                TotalBookings = bookings.Count,
                UpcomingBookings = bookings.Count(b => b.BookingDate >= DateTime.Today && b.Status == BookingStatus.Confirmed),
                CompletedBookings = bookings.Count(b => b.Status == BookingStatus.Completed),
                CancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled),
                TotalSpent = bookings.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.TotalAmount),
                Currency = "INR",
                FavoriteActivities = favoriteActivities,
                RecentBookings = recentBookings,
                WishlistCount = 0,
                UnreadNotifications = unreadNotifications
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating customer dashboard");
            throw;
        }
    }

    public async Task<ProviderDashboardDto> GetProviderDashboardAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _context.ActivityProviders
                .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted, cancellationToken);

            if (provider == null)
                return new ProviderDashboardDto();

            var activities = await _context.Activities
                .Where(a => a.ProviderId == provider.Id && !a.IsDeleted)
                .ToListAsync(cancellationToken);

            var bookings = await _context.Bookings
                .Where(b => b.Activity.ProviderId == provider.Id && !b.IsDeleted)
                .ToListAsync(cancellationToken);

            var reviews = await _context.Reviews
                .Where(r => r.ProviderId == provider.Id && !r.IsDeleted)
                .ToListAsync(cancellationToken);

            var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var topActivities = activities
                .Select(a => new
                {
                    Activity = a,
                    BookingCount = bookings.Count(b => b.ActivityId == a.Id),
                    Revenue = bookings.Where(b => b.ActivityId == a.Id && b.Status == BookingStatus.Completed).Sum(b => b.TotalAmount)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .Select(x => new TopActivityDto
                {
                    ActivityId = x.Activity.Id,
                    Title = x.Activity.Title,
                    BookingCount = x.BookingCount,
                    Revenue = x.Revenue,
                    Percentage = 0 // Will calculate below
                })
                .ToList();

            var totalRevenue = bookings.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.TotalAmount);
            foreach (var activity in topActivities)
            {
                activity.Percentage = totalRevenue > 0 ? (activity.Revenue / totalRevenue) * 100 : 0;
            }

            var recentBookings = bookings
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new ProviderRecentBookingDto
                {
                    BookingId = b.Id,
                    CustomerName = $"{b.Customer.User.FirstName} {b.Customer.User.LastName}",
                    ActivityTitle = b.Activity.Title,
                    BookingDate = b.BookingDate,
                    Status = b.Status.ToString(),
                    Amount = b.TotalAmount
                })
                .ToList();

            return new ProviderDashboardDto
            {
                TotalActivities = activities.Count,
                ActiveActivities = activities.Count(a => a.IsActive),
                PublishedActivities = activities.Count(a => a.PublishedAt.HasValue && a.IsActive),
                TotalBookings = bookings.Count,
                MonthlyBookings = bookings.Count(b => b.CreatedAt >= thisMonthStart),
                PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending),
                UpcomingBookings = bookings.Count(b => b.BookingDate >= DateTime.Today && b.Status == BookingStatus.Confirmed),
                TotalRevenue = totalRevenue,
                MonthlyRevenue = bookings.Where(b => b.Status == BookingStatus.Completed && b.CompletedAt >= thisMonthStart).Sum(b => b.TotalAmount),
                Currency = "INR",
                AverageRating = reviews.Any() ? Math.Round((decimal)reviews.Average(r => r.Rating), 1) : 0,
                TotalReviews = reviews.Count,
                TopActivities = topActivities,
                RecentBookings = recentBookings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating provider dashboard");
            throw;
        }
    }

    public async Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(
        Guid providerId,
        DateTime fromDate,
        DateTime toDate,
        string period,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var completedBookings = await _context.Bookings
                .Where(b => b.Activity.ProviderId == providerId &&
                           b.Status == BookingStatus.Completed &&
                           b.CompletedAt >= fromDate &&
                           b.CompletedAt <= toDate &&
                           !b.IsDeleted)
                .Include(b => b.Activity)
                .ToListAsync(cancellationToken);

            var totalRevenue = completedBookings.Sum(b => b.TotalAmount);

            var topActivities = completedBookings
                .GroupBy(b => new { b.ActivityId, b.Activity.Title })
                .Select(g => new TopActivityDto
                {
                    ActivityId = g.Key.ActivityId,
                    Title = g.Key.Title,
                    BookingCount = g.Count(),
                    Revenue = g.Sum(b => b.TotalAmount),
                    Percentage = totalRevenue > 0 ? (g.Sum(b => b.TotalAmount) / totalRevenue) * 100 : 0
                })
                .OrderByDescending(a => a.Revenue)
                .Take(10)
                .ToList();

            return new RevenueAnalyticsDto
            {
                TotalRevenue = totalRevenue,
                Currency = "INR",
                Period = period,
                FromDate = fromDate,
                ToDate = toDate,
                ChartData = new List<RevenueChartDataDto>(), // Simplified for now
                TopActivities = topActivities,
                AverageBookingValue = completedBookings.Any() ? totalRevenue / completedBookings.Count : 0,
                TotalBookings = completedBookings.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating revenue analytics");
            throw;
        }
    }

    public async Task<BookingTrendsDto> GetBookingTrendsAsync(
        Guid? providerId,
        DateTime fromDate,
        DateTime toDate,
        string groupBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Bookings.Where(b => !b.IsDeleted &&
                                                     b.CreatedAt >= fromDate &&
                                                     b.CreatedAt <= toDate);

            if (providerId.HasValue)
            {
                query = query.Where(b => b.Activity.ProviderId == providerId.Value);
            }

            var bookings = await query.ToListAsync(cancellationToken);

            return new BookingTrendsDto
            {
                GroupBy = groupBy,
                FromDate = fromDate,
                ToDate = toDate,
                TrendData = new List<BookingTrendDataDto>(), // Simplified for now
                TotalBookings = bookings.Count,
                GrowthPercentage = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating booking trends");
            throw;
        }
    }

    public async Task<List<ActivityPerformanceDto>> GetTopActivitiesAsync(
        Guid? providerId,
        int top = 10,
        string sortBy = "revenue",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Activities
                .Where(a => !a.IsDeleted);

            if (providerId.HasValue)
            {
                query = query.Where(a => a.ProviderId == providerId.Value);
            }

            var activities = await query
                .Include(a => a.Bookings)
                .ToListAsync(cancellationToken);

            var result = activities
                .Select(a => new ActivityPerformanceDto
                {
                    ActivityId = a.Id,
                    Title = a.Title,
                    BookingCount = a.Bookings.Count(b => !b.IsDeleted),
                    Revenue = a.Bookings.Where(b => b.Status == BookingStatus.Completed && !b.IsDeleted).Sum(b => b.TotalAmount),
                    AverageRating = a.AverageRating,
                    ViewCount = a.ViewCount,
                    ConversionRate = a.ViewCount > 0 ? ((decimal)a.Bookings.Count(b => !b.IsDeleted) / a.ViewCount) * 100 : 0
                })
                .OrderByDescending(a => sortBy.ToLower() == "bookings" ? a.BookingCount : a.Revenue)
                .Take(top)
                .ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top activities");
            throw;
        }
    }

    public async Task<ProviderPerformanceDto> GetProviderPerformanceAsync(
        Guid providerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _context.ActivityProviders
                .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);

            if (provider == null)
                throw new InvalidOperationException("Provider not found");

            var from = fromDate ?? DateTime.UtcNow.AddMonths(-12);
            var to = toDate ?? DateTime.UtcNow;

            var bookings = await _context.Bookings
                .Where(b => b.Activity.ProviderId == providerId &&
                           b.CreatedAt >= from &&
                           b.CreatedAt <= to &&
                           !b.IsDeleted)
                .ToListAsync(cancellationToken);

            var reviews = await _context.Reviews
                .Where(r => r.ProviderId == providerId && !r.IsDeleted)
                .ToListAsync(cancellationToken);

            var activeActivities = await _context.Activities
                .CountAsync(a => a.ProviderId == providerId && a.IsActive && !a.IsDeleted, cancellationToken);

            var totalBookings = bookings.Count;
            var completedBookings = bookings.Count(b => b.Status == BookingStatus.Completed);
            var cancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled);

            var confirmedBookings = bookings.Where(b => b.ConfirmedAt.HasValue).ToList();
            var avgResponseTime = confirmedBookings.Any()
                ? TimeSpan.FromMinutes(confirmedBookings.Average(b => (b.ConfirmedAt!.Value - b.CreatedAt).TotalMinutes))
                : TimeSpan.Zero;

            var totalViews = await _context.Activities
                .Where(a => a.ProviderId == providerId && !a.IsDeleted)
                .SumAsync(a => a.ViewCount, cancellationToken);

            return new ProviderPerformanceDto
            {
                ProviderId = providerId,
                BusinessName = provider.BusinessName,
                TotalRevenue = bookings.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.TotalAmount),
                TotalBookings = totalBookings,
                AverageRating = reviews.Any() ? Math.Round((decimal)reviews.Average(r => r.Rating), 1) : 0,
                TotalReviews = reviews.Count,
                CompletionRate = totalBookings > 0 ? ((decimal)completedBookings / totalBookings) * 100 : 0,
                CancellationRate = totalBookings > 0 ? ((decimal)cancelledBookings / totalBookings) * 100 : 0,
                AverageResponseTime = avgResponseTime,
                ActiveActivities = activeActivities,
                ConversionRate = totalViews > 0 ? ((decimal)totalBookings / totalViews) * 100 : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating provider performance");
            throw;
        }
    }
}