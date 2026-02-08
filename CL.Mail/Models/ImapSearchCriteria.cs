namespace CL.Mail.Models;

/// <summary>
/// Search criteria for IMAP message queries
/// </summary>
public class ImapSearchCriteria
{
    /// <summary>
    /// Gets or sets the subject filter (contains)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the from address filter (contains)
    /// </summary>
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the to address filter (contains)
    /// </summary>
    public string? To { get; set; }

    /// <summary>
    /// Gets or sets the body text filter (contains)
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the minimum date filter (messages on or after this date)
    /// </summary>
    public DateTime? Since { get; set; }

    /// <summary>
    /// Gets or sets the maximum date filter (messages before this date)
    /// </summary>
    public DateTime? Before { get; set; }

    /// <summary>
    /// Gets or sets flags that must be present
    /// </summary>
    public MessageFlags? HasFlags { get; set; }

    /// <summary>
    /// Gets or sets flags that must not be present
    /// </summary>
    public MessageFlags? NotFlags { get; set; }

    /// <summary>
    /// Gets or sets whether to include deleted messages (default: false)
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>
    /// Creates criteria that matches all messages
    /// </summary>
    public static ImapSearchCriteria All() => new();

    /// <summary>
    /// Creates criteria that matches only unread messages
    /// </summary>
    public static ImapSearchCriteria Unread() => new() { NotFlags = MessageFlags.Seen };
}
