using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// CustomerProfile entity - Represents customer profile with preferences and emergency contacts
/// Responsible for: Customer personal information, preferences, emergency contacts
/// </summary>
public class CustomerProfile : AuditableEntity
{
    private CustomerProfile() { } // Private constructor for EF Core

    // User relationship
    public Guid UserId { get; private set; }
    public virtual User User { get; private set; } = null!;

    // Personal information
    public DateTime? DateOfBirth { get; private set; }
    public string? Gender { get; private set; }
    public string? ProfilePictureUrl { get; private set; }
    public string? Bio { get; private set; }

    // Health and dietary information
    public string? DietaryRestrictions { get; private set; }
    public string? MedicalConditions { get; private set; }

    // Address information
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Country { get; private set; }
    public string? PostalCode { get; private set; }

    // Emergency contact
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public string? EmergencyContactRelationship { get; private set; }

    // Preferences
    public string? PreferredLanguage { get; private set; }
    public string? PreferredCurrency { get; private set; }
    public bool EmailNotifications { get; private set; } = true;
    public bool SmsNotifications { get; private set; } = false;
    public bool MarketingEmails { get; private set; } = false;

    // Statistics
    public int TotalBookings { get; private set; } = 0;
    public decimal TotalSpent { get; private set; } = 0;
    public DateTime? LastBookingAt { get; private set; }

    // Navigation properties
    public virtual ICollection<Booking> Bookings { get; private set; } = new List<Booking>();
    public virtual ICollection<Review> Reviews { get; private set; } = new List<Review>();
    public virtual ICollection<Wishlist> Wishlists { get; private set; } = new List<Wishlist>();

    /// <summary>
    /// Factory method to create a new customer profile
    /// </summary>
    public static CustomerProfile Create(
        Guid userId,
        DateTime? dateOfBirth = null,
        string? gender = null,
        string? emergencyContactName = null,
        string? emergencyContactPhone = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        if (dateOfBirth.HasValue && dateOfBirth.Value > DateTime.Today)
            throw new ArgumentException("Date of birth cannot be in the future", nameof(dateOfBirth));

        var profile = new CustomerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DateOfBirth = dateOfBirth,
            Gender = gender?.Trim(),
            EmergencyContactName = emergencyContactName?.Trim(),
            EmergencyContactPhone = emergencyContactPhone?.Trim(),
            EmailNotifications = true,
            SmsNotifications = false,
            MarketingEmails = false,
            TotalBookings = 0,
            TotalSpent = 0
        };

        return profile;
    }

    /// <summary>
    /// Update personal information
    /// </summary>
    public void UpdatePersonalInfo(
        DateTime? dateOfBirth,
        string? gender,
        string? bio,
        string? profilePictureUrl,
        string? dietaryRestrictions = null,
        string? medicalConditions = null)
    {
        if (dateOfBirth.HasValue && dateOfBirth.Value > DateTime.Today)
            throw new ArgumentException("Date of birth cannot be in the future", nameof(dateOfBirth));

        DateOfBirth = dateOfBirth;
        Gender = gender?.Trim();
        Bio = bio?.Trim();
        ProfilePictureUrl = profilePictureUrl?.Trim();
        DietaryRestrictions = dietaryRestrictions?.Trim();
        MedicalConditions = medicalConditions?.Trim();
    }

    /// <summary>
    /// Update address information
    /// </summary>
    public void UpdateAddress(
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? state,
        string? country,
        string? postalCode)
    {
        AddressLine1 = addressLine1?.Trim();
        AddressLine2 = addressLine2?.Trim();
        City = city?.Trim();
        State = state?.Trim();
        Country = country?.Trim();
        PostalCode = postalCode?.Trim();
    }

    /// <summary>
    /// Update emergency contact
    /// </summary>
    public void UpdateEmergencyContact(
        string? name,
        string? phone,
        string? relationship)
    {
        EmergencyContactName = name?.Trim();
        EmergencyContactPhone = phone?.Trim();
        EmergencyContactRelationship = relationship?.Trim();
    }

    /// <summary>
    /// Update notification preferences
    /// </summary>
    public void UpdateNotificationPreferences(
        bool emailNotifications,
        bool smsNotifications,
        bool marketingEmails)
    {
        EmailNotifications = emailNotifications;
        SmsNotifications = smsNotifications;
        MarketingEmails = marketingEmails;
    }

    /// <summary>
    /// Update language and currency preferences
    /// </summary>
    public void UpdatePreferences(string? preferredLanguage, string? preferredCurrency)
    {
        PreferredLanguage = preferredLanguage?.Trim();
        PreferredCurrency = preferredCurrency?.Trim();
    }

    /// <summary>
    /// Update basic profile information (convenience method)
    /// </summary>
    public void UpdateBasicInfo(
        DateTime? dateOfBirth,
        string? gender,
        string? bio,
        string? profilePictureUrl)
    {
        if (dateOfBirth.HasValue && dateOfBirth.Value > DateTime.Today)
            throw new ArgumentException("Date of birth cannot be in the future", nameof(dateOfBirth));

        DateOfBirth = dateOfBirth;
        Gender = gender?.Trim();
        Bio = bio?.Trim();
        ProfilePictureUrl = profilePictureUrl?.Trim();
    }

    /// <summary>
    /// Update dietary restrictions
    /// </summary>
    public void UpdateDietaryRestrictions(string? dietaryRestrictions)
    {
        DietaryRestrictions = dietaryRestrictions?.Trim();
    }

    /// <summary>
    /// Update medical conditions
    /// </summary>
    public void UpdateMedicalConditions(string? medicalConditions)
    {
        MedicalConditions = medicalConditions?.Trim();
    }

    /// <summary>
    /// Update preferred language
    /// </summary>
    public void UpdatePreferredLanguage(string? preferredLanguage)
    {
        PreferredLanguage = preferredLanguage?.Trim();
    }

    /// <summary>
    /// Record a new booking
    /// </summary>
    public void RecordBooking(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        TotalBookings++;
        TotalSpent += amount;
        LastBookingAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if customer has emergency contact
    /// </summary>
    public bool HasEmergencyContact()
    {
        return !string.IsNullOrWhiteSpace(EmergencyContactName) &&
               !string.IsNullOrWhiteSpace(EmergencyContactPhone);
    }

    /// <summary>
    /// Check if customer is a returning customer
    /// </summary>
    public bool IsReturningCustomer()
    {
        return TotalBookings > 0;
    }

    /// <summary>
    /// Get customer age
    /// </summary>
    public int? GetAge()
    {
        if (!DateOfBirth.HasValue)
            return null;

        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Value.Year;

        if (DateOfBirth.Value.Date > today.AddYears(-age))
            age--;

        return age;
    }
}
