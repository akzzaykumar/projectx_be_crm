using ActivoosCRM.Domain.Common;
using ActivoosCRM.Domain.Enums;
using System.Text.Json;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Pricing rule entity for dynamic pricing
/// </summary>
public class PricingRule : AuditableEntity
{
    private PricingRule() { } // Private constructor for EF Core

    public Guid ActivityId { get; private set; }
    public virtual Activity Activity { get; private set; } = null!;

    public PricingRuleType RuleType { get; private set; }
    public string RuleName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    
    // Store conditions as JSON for flexibility
    public string ConditionJson { get; private set; } = "{}";
    
    public decimal? DiscountPercentage { get; private set; }
    public decimal? MarkupPercentage { get; private set; }
    
    public int Priority { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;
    
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }

    /// <summary>
    /// Factory method to create a pricing rule
    /// </summary>
    public static PricingRule Create(
        Guid activityId,
        PricingRuleType ruleType,
        string ruleName,
        string conditionJson,
        decimal? discountPercentage = null,
        decimal? markupPercentage = null,
        int priority = 0)
    {
        if (activityId == Guid.Empty)
            throw new ArgumentException("Activity ID is required", nameof(activityId));

        if (string.IsNullOrWhiteSpace(ruleName))
            throw new ArgumentException("Rule name is required", nameof(ruleName));

        if (discountPercentage.HasValue && (discountPercentage < 0 || discountPercentage > 100))
            throw new ArgumentException("Discount percentage must be between 0 and 100", nameof(discountPercentage));

        if (markupPercentage.HasValue && markupPercentage < 0)
            throw new ArgumentException("Markup percentage cannot be negative", nameof(markupPercentage));

        if (!discountPercentage.HasValue && !markupPercentage.HasValue)
            throw new ArgumentException("Either discount or markup percentage must be specified");

        // Validate JSON
        try
        {
            JsonDocument.Parse(conditionJson);
        }
        catch
        {
            throw new ArgumentException("Invalid JSON format for conditions", nameof(conditionJson));
        }

        return new PricingRule
        {
            Id = Guid.NewGuid(),
            ActivityId = activityId,
            RuleType = ruleType,
            RuleName = ruleName.Trim(),
            ConditionJson = conditionJson,
            DiscountPercentage = discountPercentage,
            MarkupPercentage = markupPercentage,
            Priority = priority,
            IsActive = true
        };
    }

    /// <summary>
    /// Update rule conditions
    /// </summary>
    public void UpdateConditions(string conditionJson)
    {
        // Validate JSON
        try
        {
            JsonDocument.Parse(conditionJson);
        }
        catch
        {
            throw new ArgumentException("Invalid JSON format for conditions", nameof(conditionJson));
        }

        ConditionJson = conditionJson;
    }

    /// <summary>
    /// Update pricing adjustments
    /// </summary>
    public void UpdatePricing(decimal? discountPercentage, decimal? markupPercentage)
    {
        if (discountPercentage.HasValue && (discountPercentage < 0 || discountPercentage > 100))
            throw new ArgumentException("Discount percentage must be between 0 and 100", nameof(discountPercentage));

        if (markupPercentage.HasValue && markupPercentage < 0)
            throw new ArgumentException("Markup percentage cannot be negative", nameof(markupPercentage));

        if (!discountPercentage.HasValue && !markupPercentage.HasValue)
            throw new ArgumentException("Either discount or markup percentage must be specified");

        DiscountPercentage = discountPercentage;
        MarkupPercentage = markupPercentage;
    }

    /// <summary>
    /// Set validity period
    /// </summary>
    public void SetValidityPeriod(DateTime? validFrom, DateTime? validUntil)
    {
        if (validFrom.HasValue && validUntil.HasValue && validFrom > validUntil)
            throw new ArgumentException("Valid from date must be before valid until date");

        ValidFrom = validFrom;
        ValidUntil = validUntil;
    }

    /// <summary>
    /// Activate rule
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivate rule
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Update priority
    /// </summary>
    public void UpdatePriority(int priority)
    {
        Priority = priority;
    }

    /// <summary>
    /// Check if rule is valid at given date/time
    /// </summary>
    public bool IsValidAt(DateTime dateTime)
    {
        if (!IsActive)
            return false;

        if (ValidFrom.HasValue && dateTime < ValidFrom.Value)
            return false;

        if (ValidUntil.HasValue && dateTime > ValidUntil.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Calculate price adjustment multiplier
    /// </summary>
    public decimal GetPriceMultiplier()
    {
        if (DiscountPercentage.HasValue)
            return 1 - (DiscountPercentage.Value / 100);
        
        if (MarkupPercentage.HasValue)
            return 1 + (MarkupPercentage.Value / 100);

        return 1;
    }
}