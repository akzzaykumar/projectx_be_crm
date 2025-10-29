namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a password reset email
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="resetToken">Password reset token</param>
    /// <param name="userName">User's name for personalization</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendPasswordResetEmailAsync(string email, string resetToken, string userName);

    /// <summary>
    /// Send an email verification email
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="verificationToken">Email verification token</param>
    /// <param name="userName">User's name for personalization</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendEmailVerificationAsync(string email, string verificationToken, string userName);

    /// <summary>
    /// Send a welcome email to new users
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="userName">User's name for personalization</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendWelcomeEmailAsync(string email, string userName);

    /// <summary>
    /// Send a generic email
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlContent">HTML content of the email</param>
    /// <param name="textContent">Plain text content (optional)</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendEmailAsync(string to, string subject, string htmlContent, string? textContent = null);
}