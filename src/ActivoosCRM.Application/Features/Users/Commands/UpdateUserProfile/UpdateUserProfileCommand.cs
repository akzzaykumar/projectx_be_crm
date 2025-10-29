using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Users.Commands.UpdateUserProfile;

/// <summary>
/// Command to update user profile
/// </summary>
public class UpdateUserProfileCommand : IRequest<Result<UpdateUserProfileResponse>>
{
    /// <summary>
    /// User ID (set from JWT claims)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Customer profile details
    /// </summary>
    public UpdateCustomerProfileDto? CustomerProfile { get; set; }
}

/// <summary>
/// Customer profile update DTO
/// </summary>
public class UpdateCustomerProfileDto
{
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? DietaryRestrictions { get; set; }
    public string? MedicalConditions { get; set; }
    public string? PreferredLanguage { get; set; }
}

/// <summary>
/// Update user profile response
/// </summary>
public class UpdateUserProfileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
