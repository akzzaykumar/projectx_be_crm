using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Customer entity representing CRM customers
/// </summary>
public class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Status { get; set; } = "Active"; // Active, Inactive
    public string? Notes { get; set; }

    // Computed fields (can be calculated in queries)
    public int TotalBookings { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastBookingDate { get; set; }

    // Navigation properties
    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
