using CodeLogic.Logging;
using CL.Mail.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CL.Mail.Services;

/// <summary>
/// SMTP email service for sending emails using MailKit
/// </summary>
public class SmtpService : IDisposable
{
    private readonly SmtpConfiguration _config;
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the SmtpService
    /// </summary>
    public SmtpService(SmtpConfiguration config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends an email message
    /// </summary>
    public async Task<MailResult> SendAsync(Models.MailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var configValidation = ValidateConfiguration();
            if (!configValidation.IsSuccess)
                return configValidation;

            var messageValidation = ValidateMessage(message);
            if (!messageValidation.IsSuccess)
                return messageValidation;

            var mimeMessage = CreateMimeMessage(message);

            using var client = new SmtpClient();
            client.Timeout = _config.TimeoutSeconds * 1000;

            var secureOption = MapSecurityMode(_config.SecurityMode);

            await client.ConnectAsync(_config.Host, _config.Port, secureOption, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(_config.Username))
            {
                await client.AuthenticateAsync(_config.Username, _config.Password, cancellationToken).ConfigureAwait(false);
            }

            var response = await client.SendAsync(mimeMessage, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

            _logger.Info($"Email sent to {string.Join(", ", message.To)}");
            return MailResult.Success(mimeMessage.MessageId);
        }
        catch (SmtpCommandException ex)
        {
            _logger.Error("SMTP command error", ex);
            return MailResult.Failure(MailError.SmtpRejected, ex.Message);
        }
        catch (SmtpProtocolException ex)
        {
            _logger.Error("SMTP protocol error", ex);
            return MailResult.Failure(MailError.SmtpError, ex.Message);
        }
        catch (AuthenticationException ex)
        {
            _logger.Error("SMTP authentication failed", ex);
            return MailResult.Failure(MailError.SmtpAuthenticationFailed, "SMTP authentication failed");
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Email send operation cancelled");
            return MailResult.Failure(MailError.Unknown, "Operation cancelled");
        }
        catch (TimeoutException)
        {
            _logger.Error("Email send operation timed out");
            return MailResult.Failure(MailError.SmtpTimeout, "SMTP timeout");
        }
        catch (Exception ex)
        {
            _logger.Error("Unexpected error sending email", ex);
            return MailResult.Failure(MailError.Unknown, ex.Message);
        }
    }

    /// <summary>
    /// Validates the SMTP configuration
    /// </summary>
    private MailResult ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_config.Host))
            return MailResult.Failure(MailError.SmtpConfigInvalid, "SMTP host not configured");

        if (_config.Port <= 0 || _config.Port > 65535)
            return MailResult.Failure(MailError.SmtpConfigInvalid, "SMTP port is invalid");

        if (string.IsNullOrWhiteSpace(_config.Username))
            return MailResult.Failure(MailError.SmtpConfigInvalid, "SMTP username not configured");

        if (string.IsNullOrWhiteSpace(_config.Password))
            return MailResult.Failure(MailError.SmtpConfigInvalid, "SMTP password not configured");

        return MailResult.Success();
    }

    /// <summary>
    /// Validates a mail message
    /// </summary>
    private MailResult ValidateMessage(Models.MailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.From))
            return MailResult.Failure(MailError.InvalidSender, "Sender email not specified");

        if (message.To == null || message.To.Count == 0)
            return MailResult.Failure(MailError.InvalidRecipient, "No recipients specified");

        if (string.IsNullOrWhiteSpace(message.Subject))
            return MailResult.Failure(MailError.InvalidSubject, "Subject not specified");

        if (string.IsNullOrWhiteSpace(message.TextBody) && string.IsNullOrWhiteSpace(message.HtmlBody))
            return MailResult.Failure(MailError.InvalidRecipient, "Message body is empty");

        return MailResult.Success();
    }

    /// <summary>
    /// Creates a MimeMessage from a MailMessage record
    /// </summary>
    private MimeMessage CreateMimeMessage(Models.MailMessage message)
    {
        var mime = new MimeMessage();

        mime.From.Add(new MailboxAddress(message.FromName ?? message.From, message.From));

        foreach (var to in message.To)
            mime.To.Add(MailboxAddress.Parse(to));

        foreach (var cc in message.Cc)
            mime.Cc.Add(MailboxAddress.Parse(cc));

        foreach (var bcc in message.Bcc)
            mime.Bcc.Add(MailboxAddress.Parse(bcc));

        mime.Subject = message.Subject;

        // Set priority
        mime.Importance = message.Priority switch
        {
            Models.MailPriority.Low => MessageImportance.Low,
            Models.MailPriority.High => MessageImportance.High,
            _ => MessageImportance.Normal
        };

        // Add custom headers
        foreach (var header in message.Headers)
        {
            mime.Headers.Add(header.Key, header.Value);
        }

        // Build body with MimeKit BodyBuilder
        var builder = new BodyBuilder();

        if (!string.IsNullOrWhiteSpace(message.TextBody))
            builder.TextBody = message.TextBody;

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
            builder.HtmlBody = message.HtmlBody;

        // Add attachments
        foreach (var attachmentPath in message.Attachments)
        {
            if (File.Exists(attachmentPath))
            {
                builder.Attachments.Add(attachmentPath);
            }
        }

        mime.Body = builder.ToMessageBody();

        return mime;
    }

    /// <summary>
    /// Maps SmtpSecurityMode to MailKit SecureSocketOptions
    /// </summary>
    private static SecureSocketOptions MapSecurityMode(SmtpSecurityMode mode) => mode switch
    {
        SmtpSecurityMode.None => SecureSocketOptions.None,
        SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
        SmtpSecurityMode.SslTls => SecureSocketOptions.SslOnConnect,
        _ => SecureSocketOptions.Auto
    };

    /// <summary>
    /// Disposes the service
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
