namespace ActivoosCRM.Application.Common.Models;

/// <summary>
/// Email configuration settings
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Email provider type (SMTP, SendGrid, etc.)
    /// </summary>
    public EmailProvider Provider { get; set; } = EmailProvider.SMTP;

    /// <summary>
    /// From email address
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// From display name
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// SMTP settings (used when Provider is SMTP)
    /// </summary>
    public SmtpSettings Smtp { get; set; } = new();

    /// <summary>
    /// SendGrid settings (used when Provider is SendGrid)
    /// </summary>
    public SendGridSettings SendGrid { get; set; } = new();

    /// <summary>
    /// Base URL for generating email links
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Email provider types
/// </summary>
public enum EmailProvider
{
    SMTP,
    SendGrid,
    AmazonSES
}

/// <summary>
/// SMTP configuration settings
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// SMTP server host
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Username for SMTP authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for SMTP authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Use default credentials
    /// </summary>
    public bool UseDefaultCredentials { get; set; } = false;
}

/// <summary>
/// SendGrid configuration settings
/// </summary>
public class SendGridSettings
{
    /// <summary>
    /// SendGrid API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}