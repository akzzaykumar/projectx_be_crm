using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Providers.Commands.CreateProvider;

/// <summary>
/// Handler for CreateProviderCommand
/// </summary>
public class CreateProviderCommandHandler : IRequestHandler<CreateProviderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreateProviderCommandHandler> _logger;

    public CreateProviderCommandHandler(
        IApplicationDbContext context,
        ILogger<CreateProviderCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(
        CreateProviderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating provider for user: {UserId}", request.UserId);

            // Validate user exists
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return Result<Guid>.CreateFailure("User not found");
            }

            // Check if user already has a provider profile
            var existingProvider = await _context.ActivityProviders
                .FirstOrDefaultAsync(p => p.UserId == request.UserId && !p.IsDeleted, cancellationToken);

            if (existingProvider != null)
            {
                _logger.LogWarning("User already has a provider profile: {UserId}", request.UserId);
                return Result<Guid>.CreateFailure("User already has a provider profile");
            }

            // Validate location if provided
            if (request.LocationId.HasValue)
            {
                var locationExists = await _context.Locations
                    .AnyAsync(l => l.Id == request.LocationId.Value && !l.IsDeleted, cancellationToken);

                if (!locationExists)
                {
                    _logger.LogWarning("Location not found: {LocationId}", request.LocationId);
                    return Result<Guid>.CreateFailure("Location not found");
                }
            }

            // Create provider using factory method
            var provider = ActivityProvider.Create(
                userId: request.UserId,
                businessName: request.BusinessName,
                businessEmail: request.BusinessEmail,
                businessPhone: request.BusinessPhone,
                description: request.Description
            );

            // Update business info if website or logo provided
            if (!string.IsNullOrWhiteSpace(request.Website) || !string.IsNullOrWhiteSpace(request.LogoUrl))
            {
                provider.UpdateBusinessInfo(
                    businessName: request.BusinessName,
                    businessEmail: request.BusinessEmail,
                    businessPhone: request.BusinessPhone,
                    website: request.Website,
                    description: request.Description,
                    logoUrl: request.LogoUrl
                );
            }

            // Set location if provided
            if (request.LocationId.HasValue)
            {
                provider.SetLocation(request.LocationId.Value);
            }

            // Set address if provided
            if (!string.IsNullOrWhiteSpace(request.AddressLine1))
            {
                provider.UpdateAddress(
                    addressLine1: request.AddressLine1,
                    addressLine2: request.AddressLine2,
                    city: request.City,
                    state: request.StateProvince,
                    country: request.Country,
                    postalCode: request.PostalCode
                );
            }

            // Set registration details if provided
            if (!string.IsNullOrWhiteSpace(request.RegistrationNumber))
            {
                provider.UpdateRegistrationDetails(
                    businessRegistrationNumber: request.RegistrationNumber,
                    taxIdentificationNumber: request.TaxId
                );
            }

            // Set payment information if provided
            if (!string.IsNullOrWhiteSpace(request.PaymentMethod) || !string.IsNullOrWhiteSpace(request.BankAccountNumber))
            {
                provider.UpdatePaymentInfo(
                    bankAccountName: request.PaymentMethod,
                    bankAccountNumber: request.BankAccountNumber,
                    bankName: request.BankName,
                    bankBranchCode: null,
                    paymentGatewayId: null
                );
            }

            // Update user role to ActivityProvider (upgrade from Customer)
            if (user.Role == Domain.Enums.UserRole.Customer)
            {
                user.ChangeRole(Domain.Enums.UserRole.ActivityProvider);
                _logger.LogInformation("User {UserId} role upgraded to ActivityProvider", user.Id);
            }

            // Add provider to context
            await _context.ActivityProviders.AddAsync(provider, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Provider created successfully with ID: {ProviderId}", provider.Id);

            return Result<Guid>.CreateSuccess(provider.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provider for user: {UserId}", request.UserId);
            return Result<Guid>.CreateFailure("An error occurred while creating the provider");
        }
    }
}
