using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// ActivitySchedule entity - Defines recurring availability schedules for activities
/// Responsible for: Time slots, days of week, available spots management
/// </summary>
public class ActivitySchedule : AuditableEntity
{
    private ActivitySchedule() { } // Private constructor for EF Core

    // Relationships
    public Guid ActivityId { get; private set; }
    public virtual Activity Activity { get; private set; } = null!;

    // Schedule details
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public List<int> DaysOfWeek { get; private set; } = new(); // 0=Sunday, 1=Monday, etc.
    public int AvailableSpots { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Factory method to create a new activity schedule
    /// </summary>
    public static ActivitySchedule Create(
        Guid activityId,
        TimeOnly startTime,
        TimeOnly endTime,
        List<int> daysOfWeek,
        int availableSpots)
    {
        if (activityId == Guid.Empty)
            throw new ArgumentException("Activity ID is required", nameof(activityId));

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time", nameof(endTime));

        if (daysOfWeek == null || !daysOfWeek.Any())
            throw new ArgumentException("At least one day of week is required", nameof(daysOfWeek));

        if (daysOfWeek.Any(d => d < 0 || d > 6))
            throw new ArgumentException("Days of week must be between 0 (Sunday) and 6 (Saturday)", nameof(daysOfWeek));

        if (availableSpots <= 0)
            throw new ArgumentException("Available spots must be greater than 0", nameof(availableSpots));

        var schedule = new ActivitySchedule
        {
            Id = Guid.NewGuid(),
            ActivityId = activityId,
            StartTime = startTime,
            EndTime = endTime,
            DaysOfWeek = daysOfWeek.Distinct().OrderBy(d => d).ToList(),
            AvailableSpots = availableSpots,
            IsActive = true
        };

        return schedule;
    }

    /// <summary>
    /// Update schedule times
    /// </summary>
    public void UpdateTimes(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time");

        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>
    /// Update days of week
    /// </summary>
    public void UpdateDaysOfWeek(List<int> daysOfWeek)
    {
        if (daysOfWeek == null || !daysOfWeek.Any())
            throw new ArgumentException("At least one day of week is required");

        if (daysOfWeek.Any(d => d < 0 || d > 6))
            throw new ArgumentException("Days of week must be between 0 (Sunday) and 6 (Saturday)");

        DaysOfWeek = daysOfWeek.Distinct().OrderBy(d => d).ToList();
    }

    /// <summary>
    /// Update available spots
    /// </summary>
    public void UpdateAvailableSpots(int spots)
    {
        if (spots <= 0)
            throw new ArgumentException("Available spots must be greater than 0");

        AvailableSpots = spots;
    }

    /// <summary>
    /// Activate schedule
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivate schedule
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Check if schedule is active on specific day
    /// </summary>
    public bool IsActiveOnDay(DayOfWeek day)
    {
        return IsActive && DaysOfWeek.Contains((int)day);
    }
}
