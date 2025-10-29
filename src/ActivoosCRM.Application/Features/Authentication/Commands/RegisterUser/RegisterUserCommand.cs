using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Enums;
using MediatR;

namespace ActivoosCRM.Application.Features.Authentication.Commands.RegisterUser;

/// <summary>
/// Command to register a new user
/// </summary>
public class RegisterUserCommand : IRequest<Result<RegisterUserResponse>>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
}