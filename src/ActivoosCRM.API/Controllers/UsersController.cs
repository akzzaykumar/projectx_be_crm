using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Application.Features.Users.Commands.ChangePassword;
using ActivoosCRM.Application.Features.Users.Commands.UpdateUserProfile;
using ActivoosCRM.Application.Features.Users.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// User management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <returns>User profile details</returns>
    /// <response code="200">User profile retrieved successfully</response>
    /// <response code="401">Unauthorized - Invalid or missing token</response>
    /// <response code="404">User not found</response>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserIdFromClaims();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponse.CreateFailure("Invalid user token", "UNAUTHORIZED"));
        }

        _logger.LogInformation("Fetching profile for user: {UserId}", userId);

        var query = new GetUserProfileQuery { UserId = userId };
        var result = await _mediator.Send(query);

        return result.Match<IActionResult>(
            onSuccess: data =>
            {
                _logger.LogInformation("Profile retrieved successfully for user: {UserId}", userId);
                return Ok(ApiResponse<UserProfileResponse>.CreateSuccess(data, "Profile retrieved successfully"));
            },
            onFailure: error =>
            {
                _logger.LogWarning("Failed to retrieve profile for user: {UserId}, Error: {Error}", userId, error);
                return NotFound(ApiResponse.CreateFailure(error, "USER_NOT_FOUND"));
            });
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    /// <param name="request">Profile update request</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized - Invalid or missing token</response>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<UpdateUserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileRequest request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponse.CreateFailure("Invalid user token", "UNAUTHORIZED"));
        }

        _logger.LogInformation("Updating profile for user: {UserId}", userId);

        var command = new UpdateUserProfileCommand
        {
            UserId = userId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            CustomerProfile = request.CustomerProfile != null ? new UpdateCustomerProfileDto
            {
                DateOfBirth = request.CustomerProfile.DateOfBirth,
                Gender = request.CustomerProfile.Gender,
                EmergencyContactName = request.CustomerProfile.EmergencyContactName,
                EmergencyContactPhone = request.CustomerProfile.EmergencyContactPhone,
                DietaryRestrictions = request.CustomerProfile.DietaryRestrictions,
                MedicalConditions = request.CustomerProfile.MedicalConditions,
                PreferredLanguage = request.CustomerProfile.PreferredLanguage
            } : null
        };

        var result = await _mediator.Send(command);

        return result.Match<IActionResult>(
            onSuccess: data =>
            {
                _logger.LogInformation("Profile updated successfully for user: {UserId}", userId);
                return Ok(ApiResponse<UpdateUserProfileResponse>.CreateSuccess(data, data.Message));
            },
            onFailure: error =>
            {
                _logger.LogWarning("Failed to update profile for user: {UserId}, Error: {Error}", userId, error);
                return BadRequest(ApiResponse.CreateFailure(error, "PROFILE_UPDATE_FAILED"));
            });
    }

    /// <summary>
    /// Change current user password
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Invalid request or incorrect current password</response>
    /// <response code="401">Unauthorized - Invalid or missing token</response>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponse.CreateFailure("Invalid user token", "UNAUTHORIZED"));
        }

        _logger.LogInformation("Changing password for user: {UserId}", userId);

        var command = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        };

        var result = await _mediator.Send(command);

        return result.Match<IActionResult>(
            onSuccess: data =>
            {
                _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
                return Ok(ApiResponse<ChangePasswordResponse>.CreateSuccess(data, data.Message));
            },
            onFailure: error =>
            {
                _logger.LogWarning("Failed to change password for user: {UserId}, Error: {Error}", userId, error);
                return BadRequest(ApiResponse.CreateFailure(error, "PASSWORD_CHANGE_FAILED"));
            });
    }

    /// <summary>
    /// Get user ID from JWT claims
    /// </summary>
    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value
                         ?? User.FindFirst("userId")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

/// <summary>
/// Update user profile request model
/// </summary>
public class UpdateUserProfileRequest
{
    /// <summary>
    /// First name
    /// </summary>
    /// <example>John</example>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    /// <example>Doe</example>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Phone number in international format
    /// </summary>
    /// <example>+919876543210</example>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Customer profile details
    /// </summary>
    public UpdateCustomerProfileRequest? CustomerProfile { get; set; }
}

/// <summary>
/// Update customer profile request model
/// </summary>
public class UpdateCustomerProfileRequest
{
    /// <summary>
    /// Date of birth
    /// </summary>
    /// <example>1990-01-15</example>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Gender
    /// </summary>
    /// <example>Male</example>
    public string? Gender { get; set; }

    /// <summary>
    /// Emergency contact name
    /// </summary>
    /// <example>Jane Doe</example>
    public string? EmergencyContactName { get; set; }

    /// <summary>
    /// Emergency contact phone
    /// </summary>
    /// <example>+919876543211</example>
    public string? EmergencyContactPhone { get; set; }

    /// <summary>
    /// Dietary restrictions
    /// </summary>
    /// <example>Vegetarian</example>
    public string? DietaryRestrictions { get; set; }

    /// <summary>
    /// Medical conditions
    /// </summary>
    /// <example>None</example>
    public string? MedicalConditions { get; set; }

    /// <summary>
    /// Preferred language
    /// </summary>
    /// <example>English</example>
    public string? PreferredLanguage { get; set; }
}

/// <summary>
/// Change password request model
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password
    /// </summary>
    /// <example>CurrentPassword123!</example>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password (minimum 8 characters with uppercase, lowercase, number and special character)
    /// </summary>
    /// <example>NewPassword123!</example>
    public string NewPassword { get; set; } = string.Empty;
}
