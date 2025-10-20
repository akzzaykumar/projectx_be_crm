using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// TimeSlot entity representing available time slots for activities
/// </summary>
public class TimeSlot : BaseEntity
{
    public int ActivityId { get; set; }
    public string Day { get; set; } = string.Empty; // Monday, Tuesday, etc.
    public string StartTime { get; set; } = string.Empty; // HH:mm format
    public string EndTime { get; set; } = string.Empty; // HH:mm format

    // Navigation properties
    public Activity Activity { get; set; } = null!;
}
