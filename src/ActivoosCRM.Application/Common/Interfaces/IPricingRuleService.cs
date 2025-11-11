using ActivoosCRM.Domain.Enums;

namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Service interface for pricing rules and dynamic pricing
/// </summary>
public interface IPricingRuleService
{
    /// <summary>
    /// Calculate effective price for an activity based on all applicable pricing rules
    /// </summary>
    Task<decimal> CalculateEffectivePriceAsync(
        Guid activityId,
        DateTime bookingDate,
        int participants,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new pricing rule for an activity
    /// </summary>
    Task<Guid> CreatePricingRuleAsync(
        Guid activityId,
        PricingRuleType ruleType,
        string ruleName,
        string conditionJson,
        decimal? discountPercentage = null,
        decimal? markupPercentage = null,
        int priority = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active pricing rules for an activity
    /// </summary>
    Task<List<PricingRuleDto>> GetActivityPricingRulesAsync(
        Guid activityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a pricing rule
    /// </summary>
    Task UpdatePricingRuleAsync(
        Guid ruleId,
        string? conditionJson = null,
        decimal? discountPercentage = null,
        decimal? markupPercentage = null,
        int? priority = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate/Deactivate a pricing rule
    /// </summary>
    Task SetPricingRuleStatusAsync(
        Guid ruleId,
        bool isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a pricing rule
    /// </summary>
    Task DeletePricingRuleAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pricing breakdown showing all applied rules
    /// </summary>
    Task<PricingBreakdownDto> GetPricingBreakdownAsync(
        Guid activityId,
        DateTime bookingDate,
        int participants,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Pricing rule DTO
/// </summary>
public class PricingRuleDto
{
    public Guid Id { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? MarkupPercentage { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

/// <summary>
/// Pricing breakdown showing all applied rules
/// </summary>
public class PricingBreakdownDto
{
    public decimal BasePrice { get; set; }
    public decimal FinalPrice { get; set; }
    public List<AppliedRuleDto> AppliedRules { get; set; } = new();
    public decimal TotalDiscount { get; set; }
    public decimal TotalMarkup { get; set; }
}

/// <summary>
/// Applied pricing rule details
/// </summary>
public class AppliedRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public decimal Multiplier { get; set; }
    public decimal PriceImpact { get; set; }
}