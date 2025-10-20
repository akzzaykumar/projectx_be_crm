using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Booking entity representing customer bookings for activities
/// </summary>
public class Booking : BaseEntity
{
    public int CustomerId { get; set; }
    public int ActivityId { get; set; }
    public DateTime Date { get; set; }
    public int Participants { get; set; }
    public decimal PricePerPerson { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Refunded
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public string? SpecialRequirements { get; set; }

    // Cancellation info
    public string? CancellationReason { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Activity Activity { get; set; } = null!;

    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
}
