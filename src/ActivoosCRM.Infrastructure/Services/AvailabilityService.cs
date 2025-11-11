using ActivoosCRM.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Infrastructure.Services;

/// <summary>
/// Implementation of availability checking service
/// </summary>
public class AvailabilityService : IAvailabilityService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(
        IApplicationDbContext context,
        ILogger<AvailabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AvailabilityCheckResult> CheckAvailabilityAsync(
        Guid activityId,
        DateTime bookingDate,
        TimeSpan bookingTime,
        int numberOfParticipants,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the day of week (0 = Sunday, 1 = Monday, etc.)
            var dayOfWeek = (int)bookingDate.DayOfWeek;

            // Convert TimeSpan to TimeOnly for comparison
            var bookingTimeOnly = TimeOnly.FromTimeSpan(bookingTime);

            // Find matching schedule for this day and time
            var schedule = await _context.ActivitySchedules
                .Where(s => s.ActivityId == activityId && s.IsActive)
                .Where(s => s.DaysOfWeek.Contains(dayOfWeek))
                .Where(s => bookingTimeOnly >= s.StartTime && bookingTimeOnly <= s.EndTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (schedule == null)
            {
                _logger.LogWarning(
                    "No schedule found for Activity {ActivityId} on day {DayOfWeek} at time {Time}",
                    activityId, dayOfWeek, bookingTime);
                
                return AvailabilityCheckResult.Unavailable(
                    "Activity is not available on the selected day or time");
            }

            // Calculate already booked spots for this date and time
            var bookedSpots = await _context.Bookings
                .Where(b => b.ActivityId == activityId)
                .Where(b => b.BookingDate.Date == bookingDate.Date)
                .Where(b => b.BookingTime == bookingTime)
                .Where(b => b.Status == Domain.Enums.BookingStatus.Pending || 
                           b.Status == Domain.Enums.BookingStatus.Confirmed)
                .SumAsync(b => b.NumberOfParticipants, cancellationToken);

            var availableSpots = schedule.AvailableSpots - bookedSpots;

            if (availableSpots < numberOfParticipants)
            {
                _logger.LogWarning(
                    "Insufficient spots for Activity {ActivityId}. Available: {Available}, Requested: {Requested}",
                    activityId, availableSpots, numberOfParticipants);
                
                return AvailabilityCheckResult.Unavailable(
                    $"Only {availableSpots} spots available. Requested: {numberOfParticipants}");
            }

            return AvailabilityCheckResult.Available(
                schedule.Id,
                availableSpots,
                schedule.StartTime.ToTimeSpan(),
                schedule.EndTime.ToTimeSpan());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for Activity {ActivityId}", activityId);
            throw;
        }
    }

    public async Task<bool> ReserveSpotsAsync(
        Guid scheduleId,
        int numberOfParticipants,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // This method would implement optimistic locking if needed
            // For now, the availability check in CreateBooking handler is sufficient
            // In high-concurrency scenarios, we could add a version column to schedules

            var schedule = await _context.ActivitySchedules
                .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);

            if (schedule == null)
            {
                _logger.LogWarning("Schedule {ScheduleId} not found", scheduleId);
                return false;
            }

            // The actual spot reservation is handled by the booking creation
            // This method can be extended for more complex reservation logic
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving spots for Schedule {ScheduleId}", scheduleId);
            return false;
        }
    }
}