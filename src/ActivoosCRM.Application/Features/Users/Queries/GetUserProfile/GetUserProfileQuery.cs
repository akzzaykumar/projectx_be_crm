using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Users.Queries.GetUserProfile;

/// <summary>
/// Query to get user profile information
/// </summary>
public class GetUserProfileQuery : IRequest<Result<UserProfileResponse>>
{
    /// <summary>
    /// User ID to get profile for
    /// </summary>
    public Guid UserId { get; set; }
}

/// <summary>
/// User profile response
/// </summary>
public class UserProfileResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public CustomerProfileDto? CustomerProfile { get; set; }
}

/// <summary>
/// Customer profile data transfer object
/// </summary>
public class CustomerProfileDto
{
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? DietaryRestrictions { get; set; }
    public string? MedicalConditions { get; set; }
    public string? PreferredLanguage { get; set; }
}
