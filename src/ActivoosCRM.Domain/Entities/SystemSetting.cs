using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// SystemSetting entity - Stores system configuration key-value pairs
/// Responsible for: Application settings, configuration management
/// </summary>
public class SystemSetting : AuditableEntity
{
    private SystemSetting() { } // Private constructor for EF Core

    // Setting details
    public string Key { get; private set; } = string.Empty;
    public string? Value { get; private set; }
    public string? Description { get; private set; }

    /// <summary>
    /// Factory method to create a new system setting
    /// </summary>
    public static SystemSetting Create(
        string key,
        string? value = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key is required", nameof(key));

        var setting = new SystemSetting
        {
            Id = Guid.NewGuid(),
            Key = key.Trim(),
            Value = value?.Trim(),
            Description = description?.Trim()
        };

        return setting;
    }

    /// <summary>
    /// Update setting value
    /// </summary>
    public void UpdateValue(string? value)
    {
        Value = value?.Trim();
    }

    /// <summary>
    /// Update description
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
    }
}
