using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Command to logout a user by invalidating their refresh token
/// </summary>
public class LogoutCommand : IRequest<Result<LogoutResponse>>
{
    /// <summary>
    /// The refresh token to invalidate
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}