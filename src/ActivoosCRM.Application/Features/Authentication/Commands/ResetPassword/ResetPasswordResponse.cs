namespace ActivoosCRM.Application.Features.Authentication.Commands.ResetPassword;

/// <summary>
/// Response for reset password operation
/// </summary>
public class ResetPasswordResponse
{
    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the password was reset
    /// </summary>
    public DateTime ResetAt { get; set; }

    /// <summary>
    /// Indicates if the password was successfully reset
    /// </summary>
    public bool Success { get; set; }
}