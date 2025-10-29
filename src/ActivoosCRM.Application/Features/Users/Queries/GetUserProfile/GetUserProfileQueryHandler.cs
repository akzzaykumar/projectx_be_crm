using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Users.Queries.GetUserProfile;

/// <summary>
/// Handler for GetUserProfileQuery
/// </summary>
public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;

    public GetUserProfileQueryHandler(
        IApplicationDbContext context,
        ILogger<GetUserProfileQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<UserProfileResponse>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching user profile for UserId: {UserId}", request.UserId);

            var user = await _context.Users
                .Include(u => u.CustomerProfile)
                .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return Result<UserProfileResponse>.CreateFailure("User not found");
            }

            var response = new UserProfileResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CustomerProfile = user.CustomerProfile != null ? new CustomerProfileDto
                {
                    DateOfBirth = user.CustomerProfile.DateOfBirth,
                    Gender = user.CustomerProfile.Gender,
                    EmergencyContactName = user.CustomerProfile.EmergencyContactName,
                    EmergencyContactPhone = user.CustomerProfile.EmergencyContactPhone,
                    DietaryRestrictions = user.CustomerProfile.DietaryRestrictions,
                    MedicalConditions = user.CustomerProfile.MedicalConditions,
                    PreferredLanguage = user.CustomerProfile.PreferredLanguage
                } : null
            };

            _logger.LogInformation("Successfully retrieved user profile for UserId: {UserId}", request.UserId);
            return Result<UserProfileResponse>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user profile for UserId: {UserId}", request.UserId);
            return Result<UserProfileResponse>.CreateFailure("An error occurred while fetching user profile");
        }
    }
}
