using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Location entity - Represents geographic locations for activities
/// Responsible for: Location data, address management, geographic search support
/// </summary>
public class Location : AuditableEntity
{
    private Location() { } // Private constructor for EF Core

    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string? PostalCode { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }

    // Geographic coordinates for map display and distance calculations
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }

    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public virtual ICollection<Activity> Activities { get; private set; } = new List<Activity>();

    // Computed property
    public string FullAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(AddressLine1)) parts.Add(AddressLine1);
            if (!string.IsNullOrWhiteSpace(AddressLine2)) parts.Add(AddressLine2);
            if (!string.IsNullOrWhiteSpace(City)) parts.Add(City);
            if (!string.IsNullOrWhiteSpace(State)) parts.Add(State);
            if (!string.IsNullOrWhiteSpace(PostalCode)) parts.Add(PostalCode);
            if (!string.IsNullOrWhiteSpace(Country)) parts.Add(Country);
            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Factory method to create a new location
    /// </summary>
    public static Location Create(
        string name,
        string city,
        string state,
        string country,
        string? postalCode = null,
        string? addressLine1 = null,
        string? addressLine2 = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? description = null)
    {
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

        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            City = city.Trim(),
            State = state.Trim(),
            Country = country.Trim(),
            PostalCode = postalCode?.Trim(),
            AddressLine1 = addressLine1?.Trim(),
            AddressLine2 = addressLine2?.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            Description = description?.Trim(),
            IsActive = true
        };

        return location;
    }

    /// <summary>
    /// Update location details
    /// </summary>
    public void Update(
        string name,
        string city,
        string state,
        string country,
        string? postalCode,
        string? addressLine1,
        string? addressLine2,
        string? description)
    {
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
        PostalCode = postalCode?.Trim();
        AddressLine1 = addressLine1?.Trim();
        AddressLine2 = addressLine2?.Trim();
        Description = description?.Trim();
    }

    /// <summary>
    /// Update geographic coordinates
    /// </summary>
    public void UpdateCoordinates(decimal latitude, decimal longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90", nameof(latitude));

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180", nameof(longitude));

        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Activate location
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivate location
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Check if location has coordinates
    /// </summary>
    public bool HasCoordinates()
    {
        return Latitude.HasValue && Longitude.HasValue;
    }
}
