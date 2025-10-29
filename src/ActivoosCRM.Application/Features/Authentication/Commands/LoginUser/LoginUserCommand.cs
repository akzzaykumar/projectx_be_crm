using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Authentication.Commands.LoginUser;

/// <summary>
/// Command for user login
/// </summary>
public class LoginUserCommand : IRequest<Result<LoginUserResponse>>
{
    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Whether to remember the user (longer token expiry)
    /// </summary>
    public bool RememberMe { get; set; } = false;
}