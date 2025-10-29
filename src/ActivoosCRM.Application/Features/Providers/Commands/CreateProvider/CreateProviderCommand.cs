using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Providers.Commands.CreateProvider;

/// <summary>
/// Command to create a new Activity Provider profile
/// </summary>
public class CreateProviderCommand : IRequest<Result<Guid>>
{
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessEmail { get; set; }
    public string? BusinessPhone { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }

    // Location
    public Guid? LocationId { get; set; }

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // Registration details
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public DateTime? RegistrationDate { get; set; }

    // Payment information
    public string? PaymentMethod { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
}
