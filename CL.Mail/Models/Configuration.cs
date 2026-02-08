using CodeLogic.Configuration;
using System.ComponentModel.DataAnnotations;

namespace CL.Mail.Models;

/// <summary>
/// Configuration for the Mail library
/// This model is auto-generated as config/mail.json when missing.
/// </summary>
[ConfigSection("mail")]
public class MailConfiguration : ConfigModelBase
{
    /// <summary>
    /// Whether this mail library is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the SMTP configuration
    /// </summary>
    public SmtpConfiguration Smtp { get; set; } = new();

    /// <summary>
    /// Gets or sets the default sender email address
    /// </summary>
    public string? DefaultFromEmail { get; set; }

    /// <summary>
    /// Gets or sets the default sender display name
    /// </summary>
    public string? DefaultFromName { get; set; }

    /// <summary>
    /// Gets or sets the directory where mail templates are stored
    /// </summary>
    public string TemplateDirectory { get; set; } = "templates/mail/";

    /// <summary>
    /// Gets or sets whether to enable HTML by default
    /// </summary>
    public bool EnableHtmlByDefault { get; set; } = true;

    /// <summary>
    /// Gets or sets the IMAP configuration (null = IMAP disabled)
    /// </summary>
    public ImapConfiguration? Imap { get; set; }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public override ConfigValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Smtp.Host))
            errors.Add("SMTP Host is required");

        if (Smtp.Port < 1 || Smtp.Port > 65535)
            errors.Add("SMTP Port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(TemplateDirectory))
            errors.Add("TemplateDirectory is required");

        if (Imap != null)
        {
            if (string.IsNullOrWhiteSpace(Imap.Host))
                errors.Add("IMAP Host is required");

            if (Imap.Port < 1 || Imap.Port > 65535)
                errors.Add("IMAP Port must be between 1 and 65535");
        }

        if (errors.Any())
            return ConfigValidationResult.Invalid(errors);

        return ConfigValidationResult.Valid();
    }
}

/// <summary>
/// SMTP server configuration
/// </summary>
public class SmtpConfiguration
{
    /// <summary>
    /// Gets or sets the SMTP server hostname
    /// </summary>
    public string Host { get; set; } = "smtp.example.com";

    /// <summary>
    /// Gets or sets the SMTP server port
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Gets or sets the SMTP username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the security mode (None, StartTls, SslTls)
    /// </summary>
    public SmtpSecurityMode SecurityMode { get; set; } = SmtpSecurityMode.StartTls;

    /// <summary>
    /// Gets or sets the connection timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to use connection pooling
    /// </summary>
    public bool UseConnectionPooling { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of pooled connections
    /// </summary>
    public int MaxPooledConnections { get; set; } = 10;
}

/// <summary>
/// IMAP server configuration
/// </summary>
public class ImapConfiguration
{
    /// <summary>
    /// Gets or sets the IMAP server hostname
    /// </summary>
    public string Host { get; set; } = "imap.example.com";

    /// <summary>
    /// Gets or sets the IMAP server port
    /// </summary>
    public int Port { get; set; } = 993;

    /// <summary>
    /// Gets or sets the IMAP username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IMAP password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the security mode (None, StartTls, SslTls)
    /// </summary>
    public SmtpSecurityMode SecurityMode { get; set; } = SmtpSecurityMode.SslTls;

    /// <summary>
    /// Gets or sets the connection timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable IDLE push notifications
    /// </summary>
    public bool EnableIdle { get; set; } = false;

    /// <summary>
    /// Gets or sets the IDLE refresh interval in minutes (RFC 2177 recommends &lt; 29 min)
    /// </summary>
    public int IdleRefreshMinutes { get; set; } = 25;
}

/// <summary>
/// SMTP security modes (also used for IMAP)
/// </summary>
public enum SmtpSecurityMode
{
    /// <summary>
    /// No security
    /// </summary>
    None,

    /// <summary>
    /// StartTLS encryption
    /// </summary>
    StartTls,

    /// <summary>
    /// SSL/TLS encryption
    /// </summary>
    SslTls
}
