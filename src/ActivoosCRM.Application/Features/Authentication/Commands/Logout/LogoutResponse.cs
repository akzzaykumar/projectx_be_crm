namespace ActivoosCRM.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Response for logout operation
/// </summary>
public class LogoutResponse
{
    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the logout occurred
    /// </summary>
    public DateTime LoggedOutAt { get; set; }
}