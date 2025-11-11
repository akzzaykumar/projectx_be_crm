namespace ActivoosCRM.Domain.Enums;

/// <summary>
/// Dynamic pricing rule types
/// </summary>
public enum PricingRuleType
{
    PeakHours,
    EarlyBird,
    LastMinute,
    GroupDiscount,
    Seasonal,
    DayOfWeek,
    AdvanceBooking
}