namespace ActivoosCRM.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Response for forgot password operation
/// </summary>
public class ForgotPasswordResponse
{
    /// <summary>
    /// Success message (intentionally vague for security)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the request was processed
    /// </summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>
    /// Email address where reset instructions were sent (masked for security)
    /// </summary>
    public string MaskedEmail { get; set; } = string.Empty;
}