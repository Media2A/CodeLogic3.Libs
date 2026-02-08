namespace CL.Mail.Models;

/// <summary>
/// Represents an email message received via IMAP
/// </summary>
public record ReceivedMessage
{
    /// <summary>
    /// Gets the Message-ID header value
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Gets the IMAP UID of the message
    /// </summary>
    public uint Uid { get; init; }

    /// <summary>
    /// Gets the sender email address
    /// </summary>
    public string? From { get; init; }

    /// <summary>
    /// Gets the sender display name
    /// </summary>
    public string? FromName { get; init; }

    /// <summary>
    /// Gets the recipient addresses
    /// </summary>
    public IReadOnlyList<string> To { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the CC recipient addresses
    /// </summary>
    public IReadOnlyList<string> Cc { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the email subject
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Gets the plain text body (null if not fetched)
    /// </summary>
    public string? TextBody { get; init; }

    /// <summary>
    /// Gets the HTML body (null if not fetched)
    /// </summary>
    public string? HtmlBody { get; init; }

    /// <summary>
    /// Gets the date the message was sent
    /// </summary>
    public DateTimeOffset Date { get; init; }

    /// <summary>
    /// Gets the message flags
    /// </summary>
    public MessageFlags Flags { get; init; } = MessageFlags.None;

    /// <summary>
    /// Gets the folder name where this message resides
    /// </summary>
    public string? Folder { get; init; }

    /// <summary>
    /// Gets the message attachments
    /// </summary>
    public IReadOnlyList<ReceivedAttachment> Attachments { get; init; } = Array.Empty<ReceivedAttachment>();
}

/// <summary>
/// Represents an attachment on a received email message
/// </summary>
public record ReceivedAttachment
{
    /// <summary>
    /// Gets the file name of the attachment
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Gets the MIME content type
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the size in bytes
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Gets the attachment content (null if not downloaded)
    /// </summary>
    public byte[]? Content { get; init; }
}

/// <summary>
/// Message flags for IMAP messages
/// </summary>
[Flags]
public enum MessageFlags
{
    /// <summary>
    /// No flags set
    /// </summary>
    None = 0,

    /// <summary>
    /// Message has been read
    /// </summary>
    Seen = 1,

    /// <summary>
    /// Message is flagged/starred
    /// </summary>
    Flagged = 2,

    /// <summary>
    /// Message has been answered/replied to
    /// </summary>
    Answered = 4,

    /// <summary>
    /// Message is marked for deletion
    /// </summary>
    Deleted = 8,

    /// <summary>
    /// Message is a draft
    /// </summary>
    Draft = 16
}
