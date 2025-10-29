using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// ProviderContact entity - Multiple contact methods for activity providers
/// Responsible for: Provider contact information (phone, email, whatsapp, etc.)
/// </summary>
public class ProviderContact : BaseEntity
{
    private ProviderContact() { } // Private constructor for EF Core

    // Relationships
    public Guid ProviderId { get; private set; }
    public virtual ActivityProvider Provider { get; private set; } = null!;

    // Contact details
    public string ContactType { get; private set; } = string.Empty; // 'phone', 'email', 'whatsapp'
    public string ContactValue { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; } = false;

    /// <summary>
    /// Factory method to create a new provider contact
    /// </summary>
    public static ProviderContact Create(
        Guid providerId,
        string contactType,
        string contactValue,
        bool isPrimary = false)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("Provider ID is required", nameof(providerId));

        if (string.IsNullOrWhiteSpace(contactType))
            throw new ArgumentException("Contact type is required", nameof(contactType));

        if (string.IsNullOrWhiteSpace(contactValue))
            throw new ArgumentException("Contact value is required", nameof(contactValue));

        var contact = new ProviderContact
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            ContactType = contactType.ToLowerInvariant().Trim(),
            ContactValue = contactValue.Trim(),
            IsPrimary = isPrimary
        };

        return contact;
    }

    /// <summary>
    /// Update contact details
    /// </summary>
    public void Update(string contactValue)
    {
        if (string.IsNullOrWhiteSpace(contactValue))
            throw new ArgumentException("Contact value is required", nameof(contactValue));

        ContactValue = contactValue.Trim();
    }

    /// <summary>
    /// Set as primary contact
    /// </summary>
    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    /// <summary>
    /// Unset as primary contact
    /// </summary>
    public void UnsetAsPrimary()
    {
        IsPrimary = false;
    }
}
