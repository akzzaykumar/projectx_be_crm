using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// BookingParticipant entity - Details of individual participants in group bookings
/// Responsible for: Participant information, requirements, contact details
/// </summary>
public class BookingParticipant : BaseEntity
{
    private BookingParticipant() { } // Private constructor for EF Core

    // Relationships
    public Guid BookingId { get; private set; }
    public virtual Booking Booking { get; private set; } = null!;

    // Participant details
    public string Name { get; private set; } = string.Empty;
    public int? Age { get; private set; }
    public string? Gender { get; private set; }
    public string? ContactPhone { get; private set; }

    // Special requirements
    public string? DietaryRestrictions { get; private set; }
    public string? MedicalConditions { get; private set; }

    /// <summary>
    /// Factory method to create a new booking participant
    /// </summary>
    public static BookingParticipant Create(
        Guid bookingId,
        string name,
        int? age = null,
        string? gender = null,
        string? contactPhone = null,
        string? dietaryRestrictions = null,
        string? medicalConditions = null)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("Booking ID is required", nameof(bookingId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (age.HasValue && age.Value < 0)
            throw new ArgumentException("Age cannot be negative", nameof(age));

        var participant = new BookingParticipant
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            Name = name.Trim(),
            Age = age,
            Gender = gender?.Trim(),
            ContactPhone = contactPhone?.Trim(),
            DietaryRestrictions = dietaryRestrictions?.Trim(),
            MedicalConditions = medicalConditions?.Trim()
        };

        return participant;
    }

    /// <summary>
    /// Update participant details
    /// </summary>
    public void Update(
        string name,
        int? age,
        string? gender,
        string? contactPhone,
        string? dietaryRestrictions,
        string? medicalConditions)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (age.HasValue && age.Value < 0)
            throw new ArgumentException("Age cannot be negative", nameof(age));

        Name = name.Trim();
        Age = age;
        Gender = gender?.Trim();
        ContactPhone = contactPhone?.Trim();
        DietaryRestrictions = dietaryRestrictions?.Trim();
        MedicalConditions = medicalConditions?.Trim();
    }
}
