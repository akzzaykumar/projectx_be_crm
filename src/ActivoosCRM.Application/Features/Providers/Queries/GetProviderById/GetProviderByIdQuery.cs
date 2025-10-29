using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Providers.Queries.GetProviderById;

/// <summary>
/// Query to get a single Activity Provider by ID with detailed information
/// </summary>
public class GetProviderByIdQuery : IRequest<Result<ProviderDetailDto>>
{
    public Guid ProviderId { get; set; }
}

/// <summary>
/// Detailed provider information DTO
/// </summary>
public class ProviderDetailDto
{
    public Guid ProviderId { get; set; }
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessEmail { get; set; }
    public string? BusinessPhone { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }

    // Location
    public ProviderLocationDto? Location { get; set; }

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // Payment information
    public string? PaymentMethod { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }

    // Verification status
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
    public DateTime? VerificationDate { get; set; }
    public string? VerifiedBy { get; set; }
    public string? RejectionReason { get; set; }

    // Statistics
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalBookings { get; set; }
    public int ActiveActivitiesCount { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Provider location DTO
/// </summary>
public class ProviderLocationDto
{
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? Country { get; set; }
}
