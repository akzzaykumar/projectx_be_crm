using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Command to refresh JWT access token using refresh token
/// </summary>
public class RefreshTokenCommand : IRequest<Result<RefreshTokenResponse>>
{
    /// <summary>
    /// The refresh token to validate and use for generating new tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether to remember the user for extended session (optional)
    /// </summary>
    public bool RememberMe { get; set; } = false;
}