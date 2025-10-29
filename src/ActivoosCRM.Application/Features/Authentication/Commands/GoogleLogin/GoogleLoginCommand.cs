using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Authentication.Commands.GoogleLogin;

public class GoogleLoginCommand : IRequest<Result<GoogleLoginResponse>>
{
    public string IdToken { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}

public class GoogleLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsNewUser { get; set; }
}
