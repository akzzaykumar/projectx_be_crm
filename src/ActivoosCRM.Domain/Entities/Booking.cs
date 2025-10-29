using ActivoosCRM.Domain.Common;
using ActivoosCRM.Domain.Enums;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Booking entity - Represents customer bookings for activities
/// Responsible for: Booking lifecycle, participant management, status tracking
/// </summary>
public class Booking : AuditableEntity
{
    private Booking() { } // Private constructor for EF Core

    // Relationships
    public Guid CustomerId { get; private set; }
    public virtual CustomerProfile Customer { get; private set; } = null!;

    public Guid ActivityId { get; private set; }
    public virtual Activity Activity { get; private set; } = null!;

    // Booking details
    public string BookingReference { get; private set; } = string.Empty;
    public DateTime BookingDate { get; private set; }
    public TimeSpan BookingTime { get; private set; }
    public int NumberOfParticipants { get; private set; }
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;

    // Pricing
    public decimal PricePerParticipant { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal DiscountAmount { get; private set; } = 0;
    public decimal TaxAmount { get; private set; } = 0;
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "INR";

    // Discount information
    public string? CouponCode { get; private set; }
    public decimal? CouponDiscountPercentage { get; private set; }

    // Additional information
    public string? SpecialRequests { get; private set; }
    public string? ParticipantNames { get; private set; }
    public string? CustomerNotes { get; private set; }
    public string? ProviderNotes { get; private set; }

    // Status tracking
    public DateTime? ConfirmedAt { get; private set; }
    public Guid? ConfirmedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public decimal RefundAmount { get; private set; } = 0;

    // Check-in tracking
    public DateTime? CheckedInAt { get; private set; }
    public bool NoShow { get; private set; } = false;

    // Navigation properties
    public virtual Payment? Payment { get; private set; }
    public virtual ICollection<BookingParticipant> Participants { get; private set; } = new List<BookingParticipant>();
    public virtual Review? Review { get; private set; }
    public virtual ICollection<CouponUsage> CouponUsages { get; private set; } = new List<CouponUsage>();
    public virtual ICollection<Notification> Notifications { get; private set; } = new List<Notification>();

    // Computed properties
    public bool IsPaid => Payment != null && Payment.Status == PaymentStatus.Completed;
    public bool CanBeCancelled => Status == BookingStatus.Pending || Status == BookingStatus.Confirmed;
    public bool IsUpcoming => BookingDate.Date >= DateTime.Today && Status == BookingStatus.Confirmed;

    /// <summary>
    /// Factory method to create a new booking
    /// </summary>
    public static Booking Create(
        Guid customerId,
        Guid activityId,
        DateTime bookingDate,
        TimeSpan bookingTime,
        int numberOfParticipants,
        decimal pricePerParticipant,
        string currency = "INR")
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required", nameof(customerId));

        if (activityId == Guid.Empty)
            throw new ArgumentException("Activity ID is required", nameof(activityId));

        if (bookingDate < DateTime.Today)
            throw new ArgumentException("Booking date cannot be in the past", nameof(bookingDate));

        if (numberOfParticipants <= 0)
            throw new ArgumentException("Number of participants must be greater than 0", nameof(numberOfParticipants));

        if (pricePerParticipant < 0)
            throw new ArgumentException("Price cannot be negative", nameof(pricePerParticipant));

        var subtotal = pricePerParticipant * numberOfParticipants;

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ActivityId = activityId,
            BookingReference = GenerateBookingReference(),
            BookingDate = bookingDate,
            BookingTime = bookingTime,
            NumberOfParticipants = numberOfParticipants,
            PricePerParticipant = pricePerParticipant,
            SubtotalAmount = subtotal,
            DiscountAmount = 0,
            TaxAmount = 0,
            TotalAmount = subtotal,
            Currency = currency.ToUpperInvariant(),
            Status = BookingStatus.Pending,
            NoShow = false
        };

        return booking;
    }

    /// <summary>
    /// Generate unique booking reference
    /// </summary>
    private static string GenerateBookingReference()
    {
        return $"BK{DateTime.UtcNow:yyyyMMdd}{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    /// <summary>
    /// Apply discount
    /// </summary>
    public void ApplyDiscount(decimal discountAmount, string? couponCode = null, decimal? couponDiscountPercentage = null)
    {
        if (discountAmount < 0)
            throw new ArgumentException("Discount amount cannot be negative", nameof(discountAmount));

        if (discountAmount > SubtotalAmount)
            throw new ArgumentException("Discount cannot exceed subtotal", nameof(discountAmount));

        DiscountAmount = discountAmount;
        CouponCode = couponCode?.Trim().ToUpperInvariant();
        CouponDiscountPercentage = couponDiscountPercentage;

        RecalculateTotal();
    }

    /// <summary>
    /// Apply tax
    /// </summary>
    public void ApplyTax(decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new ArgumentException("Tax amount cannot be negative", nameof(taxAmount));

        TaxAmount = taxAmount;
        RecalculateTotal();
    }

    /// <summary>
    /// Recalculate total amount
    /// </summary>
    private void RecalculateTotal()
    {
        TotalAmount = SubtotalAmount - DiscountAmount + TaxAmount;

        if (TotalAmount < 0)
            TotalAmount = 0;
    }

    /// <summary>
    /// Add special requests
    /// </summary>
    public void AddSpecialRequests(string requests)
    {
        if (string.IsNullOrWhiteSpace(requests))
            throw new ArgumentException("Special requests cannot be empty", nameof(requests));

        SpecialRequests = requests.Trim();
    }

    /// <summary>
    /// Add participant names
    /// </summary>
    public void AddParticipantNames(string names)
    {
        ParticipantNames = names?.Trim();
    }

    /// <summary>
    /// Add customer notes
    /// </summary>
    public void AddCustomerNotes(string notes)
    {
        CustomerNotes = notes?.Trim();
    }

    /// <summary>
    /// Add provider notes
    /// </summary>
    public void AddProviderNotes(string notes)
    {
        ProviderNotes = notes?.Trim();
    }

    /// <summary>
    /// Confirm booking
    /// </summary>
    public void Confirm(Guid confirmedBy)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException("Only pending bookings can be confirmed");

        if (confirmedBy == Guid.Empty)
            throw new ArgumentException("Confirmed by user ID is required", nameof(confirmedBy));

        Status = BookingStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        ConfirmedBy = confirmedBy;
    }

    /// <summary>
    /// Complete booking
    /// </summary>
    public void Complete()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed bookings can be completed");

        if (BookingDate.Date > DateTime.Today)
            throw new InvalidOperationException("Cannot complete booking before the booking date");

        Status = BookingStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancel booking
    /// </summary>
    public void Cancel(Guid cancelledBy, string reason)
    {
        if (!CanBeCancelled)
            throw new InvalidOperationException($"Cannot cancel booking with status {Status}");

        if (cancelledBy == Guid.Empty)
            throw new ArgumentException("Cancelled by user ID is required", nameof(cancelledBy));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required", nameof(reason));

        Status = BookingStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancelledBy = cancelledBy;
        CancellationReason = reason.Trim();
    }

    /// <summary>
    /// Process refund
    /// </summary>
    public void ProcessRefund(decimal refundAmount)
    {
        if (Status != BookingStatus.Cancelled)
            throw new InvalidOperationException("Only cancelled bookings can be refunded");

        if (refundAmount < 0)
            throw new ArgumentException("Refund amount cannot be negative", nameof(refundAmount));

        if (refundAmount > TotalAmount)
            throw new ArgumentException("Refund amount cannot exceed total amount", nameof(refundAmount));

        RefundAmount = refundAmount;
        Status = BookingStatus.Refunded;
    }

    /// <summary>
    /// Check-in customer
    /// </summary>
    public void CheckIn()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed bookings can be checked in");

        if (BookingDate.Date != DateTime.Today)
            throw new InvalidOperationException("Check-in is only allowed on the booking date");

        CheckedInAt = DateTime.UtcNow;
        NoShow = false;
    }

    /// <summary>
    /// Mark as no-show
    /// </summary>
    public void MarkAsNoShow()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed bookings can be marked as no-show");

        if (BookingDate.Date > DateTime.Today)
            throw new InvalidOperationException("Cannot mark as no-show before the booking date");

        NoShow = true;
        Status = BookingStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = "No-show";
    }

    /// <summary>
    /// Check if booking can be modified
    /// </summary>
    public bool CanBeModified()
    {
        return Status == BookingStatus.Pending &&
               BookingDate > DateTime.Today.AddHours(24);
    }

    /// <summary>
    /// Get hours until booking
    /// </summary>
    public double GetHoursUntilBooking()
    {
        var bookingDateTime = BookingDate.Add(BookingTime);
        return (bookingDateTime - DateTime.Now).TotalHours;
    }
}
