namespace CL.Mail.Models;

/// <summary>
/// Represents an IMAP mailbox folder
/// </summary>
public record MailFolder
{
    /// <summary>
    /// Gets the folder display name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the full path name of the folder
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets the total number of messages in the folder
    /// </summary>
    public int MessageCount { get; init; }

    /// <summary>
    /// Gets the number of unread messages in the folder
    /// </summary>
    public int UnreadCount { get; init; }

    /// <summary>
    /// Gets whether this folder can be selected (opened) for reading messages
    /// </summary>
    public bool CanSelect { get; init; }
}
