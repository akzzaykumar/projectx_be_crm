namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Service for checking activity availability
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Check if activity is available for booking on specified date and time
    /// </summary>
    /// <param name="activityId">Activity ID</param>
    /// <param name="bookingDate">Booking date</param>
    /// <param name="bookingTime">Booking time</param>
    /// <param name="numberOfParticipants">Number of participants</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Availability check result</returns>
    Task<AvailabilityCheckResult> CheckAvailabilityAsync(
        Guid activityId,
        DateTime bookingDate,
        TimeSpan bookingTime,
        int numberOfParticipants,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserve spots for a booking (with optimistic locking)
    /// </summary>
    Task<bool> ReserveSpotsAsync(
        Guid scheduleId,
        int numberOfParticipants,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of availability check
/// </summary>
public record AvailabilityCheckResult
{
    public bool IsAvailable { get; init; }
    public string? Reason { get; init; }
    public Guid? ScheduleId { get; init; }
    public int AvailableSpots { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }

    public static AvailabilityCheckResult Available(Guid scheduleId, int availableSpots, TimeSpan startTime, TimeSpan endTime)
    {
        return new AvailabilityCheckResult
        {
            IsAvailable = true,
            ScheduleId = scheduleId,
            AvailableSpots = availableSpots,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    public static AvailabilityCheckResult Unavailable(string reason)
    {
        return new AvailabilityCheckResult
        {
            IsAvailable = false,
            Reason = reason
        };
    }
}