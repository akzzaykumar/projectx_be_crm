using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// ActivityProvider entity - Represents business profile for activity providers
/// Responsible for: Provider business information, verification status, payment details
/// </summary>
public class ActivityProvider : AuditableEntity
{
    private ActivityProvider() { } // Private constructor for EF Core

    // User relationship
    public Guid UserId { get; private set; }
    public virtual User User { get; private set; } = null!;

    // Business information
    public string BusinessName { get; private set; } = string.Empty;
    public string? BusinessRegistrationNumber { get; private set; }
    public string? TaxIdentificationNumber { get; private set; }
    public string? BusinessEmail { get; private set; }
    public string? BusinessPhone { get; private set; }
    public string? Website { get; private set; }
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }

    // Social media
    public string? InstagramHandle { get; private set; }
    public string? FacebookUrl { get; private set; }

    // Location relationship
    public Guid? LocationId { get; private set; }
    public virtual Location? Location { get; private set; }

    // Address information (optional separate address if different from location)
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Country { get; private set; }
    public string? PostalCode { get; private set; }

    // Banking/Payment information
    public string? BankAccountName { get; private set; }
    public string? BankAccountNumber { get; private set; }
    public string? BankName { get; private set; }
    public string? BankBranchCode { get; private set; }
    public string? PaymentGatewayId { get; private set; }

    // Verification and status
    public bool IsVerified { get; private set; } = false;
    public DateTime? VerifiedAt { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? RejectionReason { get; private set; }

    // Statistics
    public decimal AverageRating { get; private set; } = 0;
    public int TotalReviews { get; private set; } = 0;
    public int TotalBookings { get; private set; } = 0;

    // Navigation properties
    public virtual ICollection<Activity> Activities { get; private set; } = new List<Activity>();
    public virtual ICollection<ProviderContact> Contacts { get; private set; } = new List<ProviderContact>();
    public virtual ICollection<Review> Reviews { get; private set; } = new List<Review>();

    /// <summary>
    /// Factory method to create a new activity provider
    /// </summary>
    public static ActivityProvider Create(
        Guid userId,
        string businessName,
        string? businessEmail = null,
        string? businessPhone = null,
        string? description = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        if (string.IsNullOrWhiteSpace(businessName))
            throw new ArgumentException("Business name is required", nameof(businessName));

        var provider = new ActivityProvider
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessName = businessName.Trim(),
            BusinessEmail = businessEmail?.Trim(),
            BusinessPhone = businessPhone?.Trim(),
            Description = description?.Trim(),
            IsVerified = false,
            IsActive = true,
            AverageRating = 0,
            TotalReviews = 0,
            TotalBookings = 0
        };

        return provider;
    }

    /// <summary>
    /// Update business information
    /// </summary>
    public void UpdateBusinessInfo(
        string businessName,
        string? businessEmail,
        string? businessPhone,
        string? website,
        string? description,
        string? logoUrl,
        string? instagramHandle = null,
        string? facebookUrl = null)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new ArgumentException("Business name is required", nameof(businessName));

        BusinessName = businessName.Trim();
        BusinessEmail = businessEmail?.Trim();
        BusinessPhone = businessPhone?.Trim();
        Website = website?.Trim();
        Description = description?.Trim();
        LogoUrl = logoUrl?.Trim();
        InstagramHandle = instagramHandle?.Trim();
        FacebookUrl = facebookUrl?.Trim();
    }

    /// <summary>
    /// Update business registration details
    /// </summary>
    public void UpdateRegistrationDetails(
        string? businessRegistrationNumber,
        string? taxIdentificationNumber)
    {
        BusinessRegistrationNumber = businessRegistrationNumber?.Trim();
        TaxIdentificationNumber = taxIdentificationNumber?.Trim();
    }

    /// <summary>
    /// Set location
    /// </summary>
    public void SetLocation(Guid? locationId)
    {
        LocationId = locationId;
    }

    /// <summary>
    /// Update business address
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
    /// Update payment information
    /// </summary>
    public void UpdatePaymentInfo(
        string? bankAccountName,
        string? bankAccountNumber,
        string? bankName,
        string? bankBranchCode,
        string? paymentGatewayId)
    {
        BankAccountName = bankAccountName?.Trim();
        BankAccountNumber = bankAccountNumber?.Trim();
        BankName = bankName?.Trim();
        BankBranchCode = bankBranchCode?.Trim();
        PaymentGatewayId = paymentGatewayId?.Trim();
    }

    /// <summary>
    /// Verify provider account
    /// </summary>
    public void Verify(Guid verifiedBy)
    {
        if (verifiedBy == Guid.Empty)
            throw new ArgumentException("Verified by user ID is required", nameof(verifiedBy));

        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        VerifiedBy = verifiedBy;
        RejectionReason = null;
    }

    /// <summary>
    /// Reject provider verification
    /// </summary>
    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required", nameof(reason));

        IsVerified = false;
        VerifiedAt = null;
        VerifiedBy = null;
        RejectionReason = reason.Trim();
    }

    /// <summary>
    /// Activate provider account
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivate provider account
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Update rating statistics
    /// </summary>
    public void UpdateRating(decimal averageRating, int totalReviews)
    {
        if (averageRating < 0 || averageRating > 5)
            throw new ArgumentException("Average rating must be between 0 and 5", nameof(averageRating));

        if (totalReviews < 0)
            throw new ArgumentException("Total reviews cannot be negative", nameof(totalReviews));

        AverageRating = averageRating;
        TotalReviews = totalReviews;
    }

    /// <summary>
    /// Increment booking count
    /// </summary>
    public void IncrementBookingCount()
    {
        TotalBookings++;
    }

    /// <summary>
    /// Check if provider can create activities
    /// </summary>
    public bool CanCreateActivities()
    {
        return IsVerified && IsActive;
    }
}
