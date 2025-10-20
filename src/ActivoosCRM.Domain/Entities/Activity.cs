using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Activity entity representing bookable activities/services
/// </summary>
public class Activity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int MaxParticipants { get; set; }
    public int Duration { get; set; } // Duration in minutes
    public string Category { get; set; } = string.Empty; // Adventure, Water Sports, Cultural, Nature, etc.
    public DateTime AvailableFrom { get; set; }
    public DateTime AvailableUntil { get; set; }
    public string? Images { get; set; } // JSON array of image URLs
    public string? Videos { get; set; } // JSON array of video URLs

    // Computed fields
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }

    // Navigation properties
    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
