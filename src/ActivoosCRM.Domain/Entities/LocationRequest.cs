using ActivoosCRM.Domain.Common;
using ActivoosCRM.Domain.Enums;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// LocationRequest entity - Represents provider requests for new locations
/// Responsible for: Location request lifecycle, approval workflow
/// </summary>
public class LocationRequest : AuditableEntity
{
    private LocationRequest() { } // Private constructor for EF Core

    // Provider relationship
    public Guid ProviderId { get; private set; }
    public virtual ActivityProvider Provider { get; private set; } = null!;

    // Requested location details
    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }

    // Request information
    public string? Reason { get; private set; }
    public LocationRequestStatus Status { get; private set; } = LocationRequestStatus.Pending;

    // Approval/Rejection tracking
    public Guid? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    // Resulting location (if approved)
    public Guid? LocationId { get; private set; }
    public virtual Location? Location { get; private set; }

    /// <summary>
    /// Factory method to create a new location request
    /// </summary>
    public static LocationRequest Create(
        Guid providerId,
        string name,
        string city,
        string state,
        string country,
        string? address = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? reason = null)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("Provider ID is required", nameof(providerId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Location name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required", nameof(state));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required", nameof(country));

        // Validate coordinates if provided
        if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
            throw new ArgumentException("Latitude must be between -90 and 90", nameof(latitude));

        if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
            throw new ArgumentException("Longitude must be between -180 and 180", nameof(longitude));

        return new LocationRequest
        {
            ProviderId = providerId,
            Name = name.Trim(),
            City = city.Trim(),
            State = state.Trim(),
            Country = country.Trim(),
            Address = address?.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            Reason = reason?.Trim(),
            Status = LocationRequestStatus.Pending
        };
    }

    /// <summary>
    /// Approve the location request and link to created location
    /// </summary>
    public void Approve(Guid reviewedBy, Guid locationId)
    {
        if (Status != LocationRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot approve location request in {Status} status");

        if (reviewedBy == Guid.Empty)
            throw new ArgumentException("Reviewer ID is required", nameof(reviewedBy));

        if (locationId == Guid.Empty)
            throw new ArgumentException("Location ID is required", nameof(locationId));

        Status = LocationRequestStatus.Approved;
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        LocationId = locationId;
        RejectionReason = null;
    }

    /// <summary>
    /// Reject the location request with a reason
    /// </summary>
    public void Reject(Guid reviewedBy, string rejectionReason)
    {
        if (Status != LocationRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot reject location request in {Status} status");

        if (reviewedBy == Guid.Empty)
            throw new ArgumentException("Reviewer ID is required", nameof(reviewedBy));

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Rejection reason is required", nameof(rejectionReason));

        Status = LocationRequestStatus.Rejected;
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        RejectionReason = rejectionReason.Trim();
    }

    /// <summary>
    /// Update request details (only if pending)
    /// </summary>
    public void UpdateDetails(
        string name,
        string city,
        string state,
        string country,
        string? address = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? reason = null)
    {
        if (Status != LocationRequestStatus.Pending)
            throw new InvalidOperationException("Cannot update location request that is not pending");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Location name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required", nameof(state));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required", nameof(country));

        Name = name.Trim();
        City = city.Trim();
        State = state.Trim();
        Country = country.Trim();
        Address = address?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        Reason = reason?.Trim();
    }
}
