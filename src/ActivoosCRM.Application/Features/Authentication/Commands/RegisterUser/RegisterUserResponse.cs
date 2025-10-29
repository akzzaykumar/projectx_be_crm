using ActivoosCRM.Domain.Enums;

namespace ActivoosCRM.Application.Features.Authentication.Commands.RegisterUser;

/// <summary>
/// Response model for user registration
/// </summary>
public class RegisterUserResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsEmailVerified { get; set; }
}