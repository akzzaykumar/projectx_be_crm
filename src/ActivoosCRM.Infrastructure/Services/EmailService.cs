using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ActivoosCRM.Infrastructure.Services;

/// <summary>
/// Service for sending emails via SMTP or SendGrid
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Send a password reset email
    /// </summary>
    public async Task<bool> SendPasswordResetEmailAsync(string email, string resetToken, string userName)
    {
        var resetUrl = $"{_emailSettings.BaseUrl}/reset-password?token={resetToken}";

        var subject = "Reset Your Password - FunBookr";
        var htmlContent = GeneratePasswordResetEmailHtml(userName, resetUrl, resetToken);
        var textContent = GeneratePasswordResetEmailText(userName, resetUrl, resetToken);

        return await SendEmailAsync(email, subject, htmlContent, textContent);
    }

    /// <summary>
    /// Send an email verification email with OTP
    /// </summary>
    public async Task<bool> SendEmailVerificationAsync(string email, string verificationToken, string userName)
    {
        var subject = "Verify Your Email Address - FunBookr";
        var htmlContent = GenerateEmailVerificationHtml(userName, verificationToken);
        var textContent = GenerateEmailVerificationText(userName, verificationToken);

        return await SendEmailAsync(email, subject, htmlContent, textContent);
    }

    /// <summary>
    /// Send a welcome email to new users
    /// </summary>
    public async Task<bool> SendWelcomeEmailAsync(string email, string userName)
    {
        var subject = "Welcome to FunBookr!";
        var htmlContent = GenerateWelcomeEmailHtml(userName);
        var textContent = GenerateWelcomeEmailText(userName);

        return await SendEmailAsync(email, subject, htmlContent, textContent);
    }

    /// <summary>
    /// Send a generic email
    /// </summary>
    public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent, string? textContent = null)
    {
        try
        {
            _logger.LogInformation("Sending email to: {Email}, Subject: {Subject}", to, subject);

            return _emailSettings.Provider switch
            {
                EmailProvider.SMTP => await SendViaSmtpAsync(to, subject, htmlContent, textContent),
                EmailProvider.SendGrid => await SendViaSendGridAsync(to, subject, htmlContent, textContent),
                _ => throw new NotSupportedException($"Email provider {_emailSettings.Provider} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to: {Email}, Subject: {Subject}", to, subject);
            return false;
        }
    }

    /// <summary>
    /// Send email via SMTP
    /// </summary>
    private async Task<bool> SendViaSmtpAsync(string to, string subject, string htmlContent, string? textContent)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (!string.IsNullOrEmpty(textContent))
            {
                bodyBuilder.TextBody = textContent;
            }
            bodyBuilder.HtmlBody = htmlContent;
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Connect to SMTP server
            await client.ConnectAsync(
                _emailSettings.Smtp.Host,
                _emailSettings.Smtp.Port,
                _emailSettings.Smtp.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(_emailSettings.Smtp.Username) &&
                !string.IsNullOrEmpty(_emailSettings.Smtp.Password))
            {
                await client.AuthenticateAsync(_emailSettings.Smtp.Username, _emailSettings.Smtp.Password);
            }

            // Send the email
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully via SMTP to: {Email}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SMTP to: {Email}", to);
            return false;
        }
    }

    /// <summary>
    /// Send email via SendGrid
    /// </summary>
    private async Task<bool> SendViaSendGridAsync(string to, string subject, string htmlContent, string? textContent)
    {
        try
        {
            var client = new SendGridClient(_emailSettings.SendGrid.ApiKey);

            var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
            var toAddress = new EmailAddress(to);

            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, textContent, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully via SendGrid to: {Email}", to);
                return true;
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid email failed. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SendGrid to: {Email}", to);
            return false;
        }
    }

    #region Email Templates

    /// <summary>
    /// Generate HTML content for password reset email
    /// </summary>
    private string GeneratePasswordResetEmailHtml(string userName, string resetUrl, string resetToken)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Your Password</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; font-size: 12px; color: #666; text-align: center; }}
        .token {{ background-color: #e9ecef; padding: 10px; font-family: monospace; word-break: break-all; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>FunBookr</h1>
        </div>
        <div class='content'>
            <h2>Reset Your Password</h2>
            <p>Hello {userName},</p>
            <p>We received a request to reset your password for your FunBookr account. Click the button below to reset your password:</p>
            <p><a href='{resetUrl}' class='button'>Reset Password</a></p>
            <p>Or copy and paste this link in your browser:</p>
            <p><a href='{resetUrl}'>{resetUrl}</a></p>
            <p>If you prefer to use the token directly:</p>
            <div class='token'>{resetToken}</div>
            <p><strong>This link will expire in 1 hour for security reasons.</strong></p>
            <p>If you didn't request this password reset, please ignore this email. Your password will remain unchanged.</p>
        </div>
        <div class='footer'>
            <p>This is an automated email from FunBookr. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generate plain text content for password reset email
    /// </summary>
    private string GeneratePasswordResetEmailText(string userName, string resetUrl, string resetToken)
    {
        return $@"
FunBookr - Reset Your Password

Hello {userName},

We received a request to reset your password for your FunBookr account.

To reset your password, please visit the following link:
{resetUrl}

Or use this reset token directly: {resetToken}

This link will expire in 1 hour for security reasons.

If you didn't request this password reset, please ignore this email. Your password will remain unchanged.

This is an automated email from FunBookr. Please do not reply to this email.
";
    }

    /// <summary>
    /// Generate HTML content for email verification with OTP
    /// </summary>
    private string GenerateEmailVerificationHtml(string userName, string otpCode)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Verify Your Email</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .otp-container {{ text-align: center; margin: 30px 0; }}
        .otp-code {{ font-size: 32px; font-weight: bold; color: #28a745; letter-spacing: 8px; background-color: #e9ecef; padding: 15px 30px; border-radius: 8px; display: inline-block; margin: 20px 0; }}
        .footer {{ padding: 20px; font-size: 12px; color: #666; text-align: center; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>FunBookr</h1>
        </div>
        <div class='content'>
            <h2>Verify Your Email Address</h2>
            <p>Hello {userName},</p>
            <p>Thank you for signing up for FunBookr! Please use the verification code below to verify your email address:</p>
            
            <div class='otp-container'>
                <div class='otp-code'>{otpCode}</div>
                <p><strong>Enter this 6-digit code in the app to verify your email</strong></p>
            </div>
            
            <div class='warning'>
                <p><strong>⏰ This code will expire in 24 hours for security reasons.</strong></p>
                <p>If you didn't request this verification code, please ignore this email.</p>
            </div>
            
            <p>Once verified, you'll be able to access all features of your FunBookr account and start booking amazing activities!</p>
        </div>
        <div class='footer'>
            <p>This is an automated email from FunBookr. Please do not reply to this email.</p>
            <p>If you're having trouble, contact our support team at support@funbookr.com</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generate plain text content for email verification with OTP
    /// </summary>
    private string GenerateEmailVerificationText(string userName, string otpCode)
    {
        return $@"
FunBookr - Verify Your Email Address

Hello {userName},

Thank you for signing up for FunBookr! Please use the verification code below to verify your email address:

VERIFICATION CODE: {otpCode}

Enter this 6-digit code in the app to verify your email.

⏰ This code will expire in 24 hours for security reasons.

If you didn't request this verification code, please ignore this email.

Once verified, you'll be able to access all features of your FunBookr account and start booking amazing activities!

This is an automated email from FunBookr. Please do not reply to this email.
If you're having trouble, contact our support team at support@funbookr.com
";
    }

    /// <summary>
    /// Generate HTML content for welcome email
    /// </summary>
    private string GenerateWelcomeEmailHtml(string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welcome to FunBookr</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #17a2b8; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; font-size: 12px; color: #666; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to FunBookr!</h1>
        </div>
        <div class='content'>
            <h2>Welcome aboard, {userName}!</h2>
            <p>We're excited to have you join the FunBookr community! 🎉</p>
            <p>FunBookr is your gateway to amazing experiences and activities. Here's what you can do:</p>
            <ul>
                <li>Discover exciting activities and experiences</li>
                <li>Book activities with ease</li>
                <li>Manage your bookings and preferences</li>
                <li>Connect with activity providers</li>
            </ul>
            <p>Ready to start exploring? Log in to your account and discover what's waiting for you!</p>
            <p>If you have any questions, our support team is here to help.</p>
        </div>
        <div class='footer'>
            <p>This is an automated email from FunBookr. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generate plain text content for welcome email
    /// </summary>
    private string GenerateWelcomeEmailText(string userName)
    {
        return $@"
Welcome to FunBookr!

Welcome aboard, {userName}!

We're excited to have you join the FunBookr community!

FunBookr is your gateway to amazing experiences and activities. Here's what you can do:
- Discover exciting activities and experiences
- Book activities with ease
- Manage your bookings and preferences
- Connect with activity providers

Ready to start exploring? Log in to your account and discover what's waiting for you!

If you have any questions, our support team is here to help.

This is an automated email from FunBookr. Please do not reply to this email.
";
    }

    #endregion
}