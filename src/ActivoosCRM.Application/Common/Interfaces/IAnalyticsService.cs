using ActivoosCRM.Domain.Enums;

namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Service interface for analytics and dashboard metrics calculation
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Get customer dashboard statistics
    /// </summary>
    Task<CustomerDashboardDto> GetCustomerDashboardAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get provider dashboard statistics
    /// </summary>
    Task<ProviderDashboardDto> GetProviderDashboardAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get revenue analytics for provider
    /// </summary>
    Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(
        Guid providerId,
        DateTime fromDate,
        DateTime toDate,
        string period, // daily, weekly, monthly, yearly
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get booking trends with aggregation
    /// </summary>
    Task<BookingTrendsDto> GetBookingTrendsAsync(
        Guid? providerId,
        DateTime fromDate,
        DateTime toDate,
        string groupBy, // day, week, month
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top performing activities
    /// </summary>
    Task<List<ActivityPerformanceDto>> GetTopActivitiesAsync(
        Guid? providerId,
        int top = 10,
        string sortBy = "revenue", // revenue, bookings, rating
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get provider performance metrics
    /// </summary>
    Task<ProviderPerformanceDto> GetProviderPerformanceAsync(
        Guid providerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Customer dashboard DTO
/// </summary>
public class CustomerDashboardDto
{
    public int TotalBookings { get; set; }
    public int UpcomingBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal TotalSpent { get; set; }
    public string Currency { get; set; } = "INR";
    public List<FavoriteActivityDto> FavoriteActivities { get; set; } = new();
    public List<RecentBookingDto> RecentBookings { get; set; } = new();
    public int WishlistCount { get; set; }
    public int UnreadNotifications { get; set; }
}

/// <summary>
/// Provider dashboard DTO
/// </summary>
public class ProviderDashboardDto
{
    public int TotalActivities { get; set; }
    public int ActiveActivities { get; set; }
    public int PublishedActivities { get; set; }
    public int TotalBookings { get; set; }
    public int MonthlyBookings { get; set; }
    public int PendingBookings { get; set; }
    public int UpcomingBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public List<TopActivityDto> TopActivities { get; set; } = new();
    public List<ProviderRecentBookingDto> RecentBookings { get; set; } = new();
}

/// <summary>
/// Revenue analytics DTO
/// </summary>
public class RevenueAnalyticsDto
{
    public decimal TotalRevenue { get; set; }
    public string Currency { get; set; } = "INR";
    public string Period { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<RevenueChartDataDto> ChartData { get; set; } = new();
    public List<TopActivityDto> TopActivities { get; set; } = new();
    public decimal AverageBookingValue { get; set; }
    public int TotalBookings { get; set; }
}

/// <summary>
/// Booking trends DTO
/// </summary>
public class BookingTrendsDto
{
    public string GroupBy { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<BookingTrendDataDto> TrendData { get; set; } = new();
    public int TotalBookings { get; set; }
    public decimal GrowthPercentage { get; set; }
}

/// <summary>
/// Provider performance DTO
/// </summary>
public class ProviderPerformanceDto
{
    public Guid ProviderId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TotalBookings { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal CancellationRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int ActiveActivities { get; set; }
    public decimal ConversionRate { get; set; }
}

/// <summary>
/// Activity performance DTO
/// </summary>
public class ActivityPerformanceDto
{
    public Guid ActivityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageRating { get; set; }
    public int ViewCount { get; set; }
    public decimal ConversionRate { get; set; }
}

/// <summary>
/// Supporting DTOs
/// </summary>
public class FavoriteActivityDto
{
    public Guid ActivityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public string? CoverImageUrl { get; set; }
}

public class RecentBookingDto
{
    public Guid BookingId { get; set; }
    public string ActivityTitle { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class TopActivityDto
{
    public Guid ActivityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal Percentage { get; set; }
}

public class ProviderRecentBookingDto
{
    public Guid BookingId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ActivityTitle { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class RevenueChartDataDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Bookings { get; set; }
}

public class BookingTrendDataDto
{
    public string Period { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
    public int NewCustomers { get; set; }
}