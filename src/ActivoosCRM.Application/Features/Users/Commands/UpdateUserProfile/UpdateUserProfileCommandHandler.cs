using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Users.Commands.UpdateUserProfile;

/// <summary>
/// Handler for UpdateUserProfileCommand
/// </summary>
public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result<UpdateUserProfileResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

    public UpdateUserProfileCommandHandler(
        IApplicationDbContext context,
        ILogger<UpdateUserProfileCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<UpdateUserProfileResponse>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating user profile for UserId: {UserId}", request.UserId);

            var user = await _context.Users
                .Include(u => u.CustomerProfile)
                .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return Result<UpdateUserProfileResponse>.CreateFailure("User not found");
            }

            // Update user basic information
            user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);

            // Update or create customer profile if provided
            if (request.CustomerProfile != null)
            {
                if (user.CustomerProfile == null)
                {
                    // Create new customer profile using factory method
                    var customerProfile = CustomerProfile.Create(
                        userId: user.Id,
                        dateOfBirth: request.CustomerProfile.DateOfBirth,
                        gender: request.CustomerProfile.Gender,
                        emergencyContactName: request.CustomerProfile.EmergencyContactName,
                        emergencyContactPhone: request.CustomerProfile.EmergencyContactPhone
                    );

                    // Set optional fields
                    if (!string.IsNullOrEmpty(request.CustomerProfile.DietaryRestrictions))
                        customerProfile.UpdateDietaryRestrictions(request.CustomerProfile.DietaryRestrictions);

                    if (!string.IsNullOrEmpty(request.CustomerProfile.MedicalConditions))
                        customerProfile.UpdateMedicalConditions(request.CustomerProfile.MedicalConditions);

                    if (!string.IsNullOrEmpty(request.CustomerProfile.PreferredLanguage))
                        customerProfile.UpdatePreferredLanguage(request.CustomerProfile.PreferredLanguage);

                    _context.CustomerProfiles.Add(customerProfile);
                }
                else
                {
                    // Update existing customer profile
                    user.CustomerProfile.UpdateBasicInfo(
                        request.CustomerProfile.DateOfBirth,
                        request.CustomerProfile.Gender,
                        request.CustomerProfile.EmergencyContactName,
                        request.CustomerProfile.EmergencyContactPhone
                    );

                    if (!string.IsNullOrEmpty(request.CustomerProfile.DietaryRestrictions))
                        user.CustomerProfile.UpdateDietaryRestrictions(request.CustomerProfile.DietaryRestrictions);

                    if (!string.IsNullOrEmpty(request.CustomerProfile.MedicalConditions))
                        user.CustomerProfile.UpdateMedicalConditions(request.CustomerProfile.MedicalConditions);

                    if (!string.IsNullOrEmpty(request.CustomerProfile.PreferredLanguage))
                        user.CustomerProfile.UpdatePreferredLanguage(request.CustomerProfile.PreferredLanguage);
                }
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User profile updated successfully for UserId: {UserId}", request.UserId);

            return Result<UpdateUserProfileResponse>.CreateSuccess(new UpdateUserProfileResponse
            {
                Success = true,
                Message = "Profile updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for UserId: {UserId}", request.UserId);
            return Result<UpdateUserProfileResponse>.CreateFailure("An error occurred while updating user profile");
        }
    }
}
