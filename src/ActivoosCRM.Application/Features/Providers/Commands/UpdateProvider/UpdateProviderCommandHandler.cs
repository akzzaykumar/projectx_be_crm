using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Providers.Commands.UpdateProvider;

/// <summary>
/// Handler for UpdateProviderCommand
/// </summary>
public class UpdateProviderCommandHandler : IRequestHandler<UpdateProviderCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateProviderCommandHandler> _logger;

    public UpdateProviderCommandHandler(
        IApplicationDbContext context,
        ILogger<UpdateProviderCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        UpdateProviderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating provider: {ProviderId} for user: {UserId}",
                request.ProviderId, request.UserId);

            // Get provider
            var provider = await _context.ActivityProviders
                .FirstOrDefaultAsync(p => p.Id == request.ProviderId && !p.IsDeleted, cancellationToken);

            if (provider == null)
            {
                _logger.LogWarning("Provider not found: {ProviderId}", request.ProviderId);
                return Result<bool>.CreateFailure("Provider not found");
            }

            // Verify ownership
            if (provider.UserId != request.UserId)
            {
                _logger.LogWarning("User {UserId} is not authorized to update provider {ProviderId}",
                    request.UserId, request.ProviderId);
                return Result<bool>.CreateFailure("You are not authorized to update this provider");
            }

            // Validate location if provided
            if (request.LocationId.HasValue)
            {
                var locationExists = await _context.Locations
                    .AnyAsync(l => l.Id == request.LocationId.Value && !l.IsDeleted, cancellationToken);

                if (!locationExists)
                {
                    _logger.LogWarning("Location not found: {LocationId}", request.LocationId);
                    return Result<bool>.CreateFailure("Location not found");
                }
            }

            // Update business information
            provider.UpdateBusinessInfo(
                businessName: request.BusinessName,
                businessEmail: request.BusinessEmail,
                businessPhone: request.BusinessPhone,
                website: request.Website,
                description: request.Description,
                logoUrl: request.LogoUrl
            );

            // Update location if provided
            if (request.LocationId.HasValue)
            {
                provider.SetLocation(request.LocationId.Value);
            }

            // Update address
            provider.UpdateAddress(
                addressLine1: request.AddressLine1,
                addressLine2: request.AddressLine2,
                city: request.City,
                state: request.StateProvince,
                country: request.Country,
                postalCode: request.PostalCode
            );

            // Update registration details if provided
            if (!string.IsNullOrWhiteSpace(request.RegistrationNumber))
            {
                provider.UpdateRegistrationDetails(
                    businessRegistrationNumber: request.RegistrationNumber,
                    taxIdentificationNumber: request.TaxId
                );
            }

            // Update payment information if provided
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

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Provider updated successfully: {ProviderId}", request.ProviderId);

            return Result<bool>.CreateSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider: {ProviderId}", request.ProviderId);
            return Result<bool>.CreateFailure("An error occurred while updating the provider");
        }
    }
}
