using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Providers.Queries.GetProviderById;

/// <summary>
/// Handler for GetProviderByIdQuery
/// </summary>
public class GetProviderByIdQueryHandler : IRequestHandler<GetProviderByIdQuery, Result<ProviderDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetProviderByIdQueryHandler> _logger;

    public GetProviderByIdQueryHandler(
        IApplicationDbContext context,
        ILogger<GetProviderByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ProviderDetailDto>> Handle(
        GetProviderByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching provider details for ID: {ProviderId}", request.ProviderId);

            var provider = await _context.ActivityProviders
                .Include(p => p.Location)
                .Include(p => p.Activities)
                .Where(p => p.Id == request.ProviderId && !p.IsDeleted)
                .Select(p => new ProviderDetailDto
                {
                    ProviderId = p.Id,
                    UserId = p.UserId,
                    BusinessName = p.BusinessName,
                    BusinessEmail = p.BusinessEmail,
                    BusinessPhone = p.BusinessPhone,
                    Description = p.Description,
                    Website = p.Website,
                    LogoUrl = p.LogoUrl,

                    Location = p.Location != null ? new ProviderLocationDto
                    {
                        LocationId = p.Location.Id,
                        Name = p.Location.Name,
                        City = p.Location.City,
                        State = p.Location.State,
                        Country = p.Location.Country
                    } : null,

                    AddressLine1 = p.AddressLine1,
                    AddressLine2 = p.AddressLine2,
                    City = p.City,
                    StateProvince = p.State,
                    PostalCode = p.PostalCode,
                    Country = p.Country,

                    PaymentMethod = p.BankAccountName,
                    BankAccountNumber = p.BankAccountNumber,
                    BankName = p.BankName,

                    IsVerified = p.IsVerified,
                    IsActive = p.IsActive,
                    VerificationDate = p.VerifiedAt,
                    VerifiedBy = p.VerifiedBy.HasValue ? p.VerifiedBy.Value.ToString() : null,
                    RejectionReason = p.RejectionReason,

                    AverageRating = p.AverageRating,
                    TotalReviews = p.TotalReviews,
                    TotalBookings = p.TotalBookings,
                    ActiveActivitiesCount = p.Activities.Count(a => a.IsActive && !a.IsDeleted),

                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (provider == null)
            {
                _logger.LogWarning("Provider not found with ID: {ProviderId}", request.ProviderId);
                return Result<ProviderDetailDto>.CreateFailure("Provider not found");
            }

            _logger.LogInformation("Successfully retrieved provider: {BusinessName}", provider.BusinessName);

            return Result<ProviderDetailDto>.CreateSuccess(provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching provider details for ID: {ProviderId}", request.ProviderId);
            return Result<ProviderDetailDto>.CreateFailure(
                "An error occurred while fetching provider details");
        }
    }
}
