using CodeLogic.Logging;
using CL.Mail.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using MkMessageFlags = MailKit.MessageFlags;

namespace CL.Mail.Services;

/// <summary>
/// Event args for new mail notifications via IMAP IDLE
/// </summary>
public class NewMailEventArgs : EventArgs
{
    /// <summary>
    /// Gets the folder name where new mail arrived
    /// </summary>
    public required string FolderName { get; init; }

    /// <summary>
    /// Gets the current message count in the folder
    /// </summary>
    public int MessageCount { get; init; }
}

/// <summary>
/// IMAP mailbox service for reading and managing emails using MailKit
/// </summary>
public class ImapService : IDisposable
{
    private readonly ImapConfiguration _config;
    private readonly ILogger _logger;
    private ImapClient? _client;
    private bool _disposed;

    // IDLE support
    private CancellationTokenSource? _idleCts;
    private Task? _idleTask;

    /// <summary>
    /// Fired when new mail arrives during IDLE monitoring
    /// </summary>
    public event EventHandler<NewMailEventArgs>? NewMailReceived;

    /// <summary>
    /// Initializes a new instance of the ImapService
    /// </summary>
    public ImapService(ImapConfiguration config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Connection

    /// <summary>
    /// Connects and authenticates to the IMAP server
    /// </summary>
    public async Task<MailResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _client = new ImapClient();
            _client.Timeout = _config.TimeoutSeconds * 1000;

            var secureOption = MapSecurityMode(_config.SecurityMode);
            await _client.ConnectAsync(_config.Host, _config.Port, secureOption, cancellationToken).ConfigureAwait(false);
            await _client.AuthenticateAsync(_config.Username, _config.Password, cancellationToken).ConfigureAwait(false);

            _logger.Info($"Connected to IMAP server {_config.Host}:{_config.Port}");
            return MailResult.Success();
        }
        catch (AuthenticationException ex)
        {
            _logger.Error("IMAP authentication failed", ex);
            return MailResult.Failure(MailError.ImapAuthenticationFailed, "IMAP authentication failed");
        }
        catch (TimeoutException ex)
        {
            _logger.Error("IMAP connection timed out", ex);
            return MailResult.Failure(MailError.ImapTimeout, "IMAP connection timed out");
        }
        catch (Exception ex)
        {
            _logger.Error("IMAP connection error", ex);
            return MailResult.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Disconnects from the IMAP server
    /// </summary>
    public async Task<MailResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            StopIdle();

            if (_client is { IsConnected: true })
            {
                await _client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
                _logger.Info("Disconnected from IMAP server");
            }

            return MailResult.Success();
        }
        catch (Exception ex)
        {
            _logger.Error("Error disconnecting from IMAP", ex);
            return MailResult.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Ensures the client is connected and authenticated, reconnecting if needed
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_client is { IsConnected: true, IsAuthenticated: true })
            return;

        _logger.Info("IMAP connection lost, reconnecting...");
        _client?.Dispose();
        var result = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Failed to reconnect to IMAP: {result.ErrorMessage}");
    }

    #endregion

    #region Folders

    /// <summary>
    /// Lists all available folders
    /// </summary>
    public async Task<MailResult<IReadOnlyList<Models.MailFolder>>> ListFoldersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var personal = _client!.GetFolder(_client.PersonalNamespaces[0]);
            var subfolders = await personal.GetSubfoldersAsync(true, cancellationToken).ConfigureAwait(false);

            var folders = new List<Models.MailFolder>();

            // Include INBOX
            var inbox = _client.Inbox;
            await inbox.StatusAsync(StatusItems.Count | StatusItems.Unread, cancellationToken).ConfigureAwait(false);
            folders.Add(new Models.MailFolder
            {
                Name = inbox.Name,
                FullName = inbox.FullName,
                MessageCount = inbox.Count,
                UnreadCount = inbox.Unread,
                CanSelect = true
            });

            foreach (var folder in subfolders)
            {
                try
                {
                    var canSelect = !folder.Attributes.HasFlag(FolderAttributes.NoSelect);
                    var msgCount = 0;
                    var unreadCount = 0;

                    if (canSelect)
                    {
                        await folder.StatusAsync(StatusItems.Count | StatusItems.Unread, cancellationToken).ConfigureAwait(false);
                        msgCount = folder.Count;
                        unreadCount = folder.Unread;
                    }

                    folders.Add(new Models.MailFolder
                    {
                        Name = folder.Name,
                        FullName = folder.FullName,
                        MessageCount = msgCount,
                        UnreadCount = unreadCount,
                        CanSelect = canSelect
                    });
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Could not get status for folder '{folder.FullName}': {ex.Message}");
                }
            }

            return MailResult<IReadOnlyList<Models.MailFolder>>.Success(folders.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.Error("Error listing IMAP folders", ex);
            return MailResult<IReadOnlyList<Models.MailFolder>>.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Creates a new folder
    /// </summary>
    public async Task<MailResult> CreateFolderAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var personal = _client!.GetFolder(_client.PersonalNamespaces[0]);
            await personal.CreateAsync(name, true, cancellationToken).ConfigureAwait(false);

            _logger.Info($"Created IMAP folder: {name}");
            return MailResult.Success();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating folder '{name}'", ex);
            return MailResult.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Deletes a folder by full name
    /// </summary>
    public async Task<MailResult> DeleteFolderAsync(string fullName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var folder = await GetFolderAsync(fullName, FolderAccess.None, cancellationToken).ConfigureAwait(false);
            if (folder == null)
                return MailResult.Failure(MailError.FolderNotFound, $"Folder '{fullName}' not found");

            await folder.DeleteAsync(cancellationToken).ConfigureAwait(false);

            _logger.Info($"Deleted IMAP folder: {fullName}");
            return MailResult.Success();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting folder '{fullName}'", ex);
            return MailResult.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Renames a folder
    /// </summary>
    public async Task<MailResult> RenameFolderAsync(string oldName, string newName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var folder = await GetFolderAsync(oldName, FolderAccess.None, cancellationToken).ConfigureAwait(false);
            if (folder == null)
                return MailResult.Failure(MailError.FolderNotFound, $"Folder '{oldName}' not found");

            var parent = folder.ParentFolder;
            await folder.RenameAsync(parent, newName, cancellationToken).ConfigureAwait(false);

            _logger.Info($"Renamed IMAP folder: {oldName} -> {newName}");
            return MailResult.Success();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error renaming folder '{oldName}' to '{newName}'", ex);
            return MailResult.Failure(MailError.ImapError, ex.Message);
        }
    }

    #endregion

    #region Messages

    /// <summary>
    /// Fetches messages from a folder with paging, newest first
    /// </summary>
    public async Task<MailResult<IReadOnlyList<ReceivedMessage>>> FetchMessagesAsync(
        string folder, int offset, int count, bool includeBody = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var imapFolder = await GetFolderAsync(folder, FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            if (imapFolder == null)
                return MailResult<IReadOnlyList<ReceivedMessage>>.Failure(MailError.FolderNotFound, $"Folder '{folder}' not found");

            var totalMessages = imapFolder.Count;
            if (totalMessages == 0 || offset >= totalMessages)
                return MailResult<IReadOnlyList<ReceivedMessage>>.Success(Array.Empty<ReceivedMessage>());

            // Newest first: calculate range from end
            var startIndex = Math.Max(0, totalMessages - offset - count);
            var endIndex = Math.Max(0, totalMessages - offset - 1);

            var range = new[] { new UniqueId((uint)(startIndex + 1), (uint)(endIndex + 1)) };

            var items = MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags;
            if (includeBody)
                items |= MessageSummaryItems.BodyStructure;

            var uids = Enumerable.Range(startIndex, endIndex - startIndex + 1)
                .Select(i => new UniqueId((uint)(i + 1)))
                .ToList();

            var summaries = await imapFolder.FetchAsync(uids, items, cancellationToken).ConfigureAwait(false);

            var messages = new List<ReceivedMessage>();
            foreach (var summary in summaries.Reverse())
            {
                var msg = await ConvertToReceivedMessage(imapFolder, summary, includeBody, cancellationToken).ConfigureAwait(false);
                messages.Add(msg);
            }

            return MailResult<IReadOnlyList<ReceivedMessage>>.Success(messages.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.Error($"Error fetching messages from '{folder}'", ex);
            return MailResult<IReadOnlyList<ReceivedMessage>>.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Gets a single message by UID with full body content
    /// </summary>
    public async Task<MailResult<ReceivedMessage>> GetMessageAsync(
        string folder, uint uid, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var imapFolder = await GetFolderAsync(folder, FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            if (imapFolder == null)
                return MailResult<ReceivedMessage>.Failure(MailError.FolderNotFound, $"Folder '{folder}' not found");

            var uniqueId = new UniqueId(uid);
            var message = await imapFolder.GetMessageAsync(uniqueId, cancellationToken).ConfigureAwait(false);

            if (message == null)
                return MailResult<ReceivedMessage>.Failure(MailError.MessageNotFound, $"Message UID {uid} not found");

            var summaries = await imapFolder.FetchAsync(
                new[] { uniqueId },
                MessageSummaryItems.UniqueId | MessageSummaryItems.Flags,
                cancellationToken).ConfigureAwait(false);

            var flags = summaries.FirstOrDefault()?.Flags ?? MkMessageFlags.None;

            var attachments = new List<ReceivedAttachment>();
            foreach (var attachment in message.Attachments)
            {
                using var stream = new MemoryStream();
                if (attachment is MimePart part)
                {
                    await part.Content.DecodeToAsync(stream, cancellationToken).ConfigureAwait(false);
                    attachments.Add(new ReceivedAttachment
                    {
                        FileName = part.FileName,
                        ContentType = part.ContentType.MimeType,
                        Size = stream.Length,
                        Content = stream.ToArray()
                    });
                }
            }

            var result = new ReceivedMessage
            {
                MessageId = message.MessageId,
                Uid = uid,
                From = message.From.Mailboxes.FirstOrDefault()?.Address,
                FromName = message.From.Mailboxes.FirstOrDefault()?.Name,
                To = message.To.Mailboxes.Select(m => m.Address).ToList(),
                Cc = message.Cc.Mailboxes.Select(m => m.Address).ToList(),
                Subject = message.Subject,
                TextBody = message.TextBody,
                HtmlBody = message.HtmlBody,
                Date = message.Date,
                Flags = ConvertFlags(flags),
                Folder = folder,
                Attachments = attachments.AsReadOnly()
            };

            return MailResult<ReceivedMessage>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting message UID {uid} from '{folder}'", ex);
            return MailResult<ReceivedMessage>.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Searches for messages in a folder
    /// </summary>
    public async Task<MailResult<IReadOnlyList<ReceivedMessage>>> SearchAsync(
        string folder, ImapSearchCriteria criteria, bool includeBody = false, int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var imapFolder = await GetFolderAsync(folder, FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            if (imapFolder == null)
                return MailResult<IReadOnlyList<ReceivedMessage>>.Failure(MailError.FolderNotFound, $"Folder '{folder}' not found");

            var query = BuildSearchQuery(criteria);
            var uids = await imapFolder.SearchAsync(query, cancellationToken).ConfigureAwait(false);

            // Take newest first, limited to maxResults
            var limitedUids = uids.Reverse().Take(maxResults).ToList();

            if (limitedUids.Count == 0)
                return MailResult<IReadOnlyList<ReceivedMessage>>.Success(Array.Empty<ReceivedMessage>());

            var items = MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags;
            if (includeBody)
                items |= MessageSummaryItems.BodyStructure;

            var summaries = await imapFolder.FetchAsync(limitedUids, items, cancellationToken).ConfigureAwait(false);

            var messages = new List<ReceivedMessage>();
            foreach (var summary in summaries.Reverse())
            {
                var msg = await ConvertToReceivedMessage(imapFolder, summary, includeBody, cancellationToken).ConfigureAwait(false);
                messages.Add(msg);
            }

            return MailResult<IReadOnlyList<ReceivedMessage>>.Success(messages.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.Error($"Error searching messages in '{folder}'", ex);
            return MailResult<IReadOnlyList<ReceivedMessage>>.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Moves a message to another folder
    /// </summary>
    public async Task<MailResult> MoveMessageAsync(
        string source, uint uid, string destination, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var sourceFolder = await GetFolderAsync(source, FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
            if (sourceFolder == null)
                return MailResult.Failure(MailError.FolderNotFound, $"Source folder '{source}' not found");

            var destFolder = await GetFolderAsync(destination, FolderAccess.None, cancellationToken).ConfigureAwait(false);
            if (destFolder == null)
                return MailResult.Failure(MailError.FolderNotFound, $"Destination folder '{destination}' not found");

            await sourceFolder.MoveToAsync(new UniqueId(uid), destFolder, cancellationToken).ConfigureAwait(false);

            _logger.Info($"Moved message UID {uid} from '{source}' to '{destination}'");
            return MailResult.Success();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error moving message UID {uid}", ex);
            return MailResult.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Copies a message to another folder
    /// </summary>
    public async Task<MailResult> CopyMessageAsync(
        string source, uint uid, string destination, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var sourceFolder = await GetFolderAsync(source, FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            if (sourceFolder == null)
                return MailResult.Failure(MailError.FolderNotFound, $"Source folder '{source}' not found");

            var destFolder = await GetFolderAsync(destination, FolderAccess.None, cancellationToken).ConfigureAwait(false);
            if (destFolder == null)
                return MailResult.Failure(MailError.FolderNotFound, $"Destination folder '{destination}' not found");

            await sourceFolder.CopyToAsync(new UniqueId(uid), destFolder, cancellationToken).ConfigureAwait(false);

            _logger.Info($"Copied message UID {uid} from '{source}' to '{destination}'");
            return MailResult.Success();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error copying message UID {uid}", ex);
            return MailResult.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Deletes a message by marking it as deleted and expunging
    /// </summary>
    public async Task<MailResult> DeleteMessageAsync(
        string folder, uint uid, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var imapFolder = await GetFolderAsync(folder, FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
            if (imapFolder == null)
                return MailResult.Failure(MailError.FolderNotFound, $"Folder '{folder}' not found");

            var uniqueId = new UniqueId(uid);
            await imapFolder.AddFlagsAsync(uniqueId, MkMessageFlags.Deleted, true, cancellationToken).ConfigureAwait(false);
            await imapFolder.ExpungeAsync(cancellationToken).ConfigureAwait(false);

            _logger.Info($"Deleted message UID {uid} from '{folder}'");
            return MailResult.Success();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting message UID {uid} from '{folder}'", ex);
            return MailResult.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Sets or removes flags on a message
    /// </summary>
    public async Task<MailResult> SetMessageFlagsAsync(
        string folder, uint uid, Models.MessageFlags flags, bool add = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var imapFolder = await GetFolderAsync(folder, FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
            if (imapFolder == null)
                return MailResult.Failure(MailError.FolderNotFound, $"Folder '{folder}' not found");

            var mkFlags = ConvertToMailKitFlags(flags);
            var uniqueId = new UniqueId(uid);

            if (add)
                await imapFolder.AddFlagsAsync(uniqueId, mkFlags, true, cancellationToken).ConfigureAwait(false);
            else
                await imapFolder.RemoveFlagsAsync(uniqueId, mkFlags, true, cancellationToken).ConfigureAwait(false);

            _logger.Info($"{(add ? "Added" : "Removed")} flags {flags} on message UID {uid} in '{folder}'");
            return MailResult.Success();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error setting flags on message UID {uid}", ex);
            return MailResult.Failure(MailError.ImapError, ex.Message);
        }
    }

    #endregion

    #region IDLE Push

    /// <summary>
    /// Starts IDLE monitoring on a folder for new mail notifications
    /// </summary>
    public async Task<MailResult> StartIdleAsync(string folder, CancellationToken cancellationToken = default)
    {
        if (_idleTask != null)
            return MailResult.Failure(MailError.ImapError, "IDLE is already running. Call StopIdle() first.");

        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            if (!_client!.Capabilities.HasFlag(ImapCapabilities.Idle))
            {
                _logger.Warning("IMAP server does not support IDLE");
                return MailResult.Failure(MailError.ImapError, "Server does not support IDLE");
            }

            var imapFolder = await GetFolderAsync(folder, FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);
            if (imapFolder == null)
                return MailResult.Failure(MailError.FolderNotFound, $"Folder '{folder}' not found");

            _idleCts = new CancellationTokenSource();
            var idleToken = _idleCts.Token;

            _idleTask = Task.Run(async () =>
            {
                var refreshInterval = TimeSpan.FromMinutes(_config.IdleRefreshMinutes);

                while (!idleToken.IsCancellationRequested)
                {
                    try
                    {
                        await EnsureConnectedAsync(idleToken).ConfigureAwait(false);

                        // Re-open folder if needed
                        if (!imapFolder.IsOpen)
                            await imapFolder.OpenAsync(FolderAccess.ReadOnly, idleToken).ConfigureAwait(false);

                        var previousCount = imapFolder.Count;

                        using var doneCts = new CancellationTokenSource(refreshInterval);
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(idleToken, doneCts.Token);

                        try
                        {
                            await _client!.IdleAsync(doneCts.Token, linkedCts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (doneCts.IsCancellationRequested && !idleToken.IsCancellationRequested)
                        {
                            // Refresh timeout - this is normal, continue the loop
                        }

                        // Check for new messages
                        if (imapFolder.Count > previousCount)
                        {
                            _logger.Info($"New mail detected in '{folder}': {imapFolder.Count} messages (was {previousCount})");
                            NewMailReceived?.Invoke(this, new NewMailEventArgs
                            {
                                FolderName = folder,
                                MessageCount = imapFolder.Count
                            });
                        }
                    }
                    catch (OperationCanceledException) when (idleToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"IDLE error, will retry: {ex.Message}");
                        try { await Task.Delay(5000, idleToken).ConfigureAwait(false); }
                        catch (OperationCanceledException) { break; }
                    }
                }

                _logger.Info("IDLE monitoring stopped");
            }, idleToken);

            _logger.Info($"Started IDLE monitoring on folder '{folder}'");
            return MailResult.Success();
        }
        catch (Exception ex)
        {
            _logger.Error("Error starting IDLE", ex);
            return MailResult.Failure(MailError.ImapError, ex.Message);
        }
    }

    /// <summary>
    /// Stops IDLE monitoring
    /// </summary>
    public void StopIdle()
    {
        if (_idleCts == null) return;

        _idleCts.Cancel();
        try { _idleTask?.Wait(TimeSpan.FromSeconds(5)); }
        catch (AggregateException) { /* expected */ }

        _idleCts.Dispose();
        _idleCts = null;
        _idleTask = null;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Resolves and optionally opens an IMAP folder by name
    /// </summary>
    private async Task<IMailFolder?> GetFolderAsync(string name, FolderAccess access, CancellationToken cancellationToken)
    {
        try
        {
            IMailFolder folder;

            if (string.Equals(name, "INBOX", StringComparison.OrdinalIgnoreCase))
            {
                folder = _client!.Inbox;
            }
            else
            {
                folder = await _client!.GetFolderAsync(name, cancellationToken).ConfigureAwait(false);
            }

            if (access != FolderAccess.None && !folder.IsOpen)
            {
                await folder.OpenAsync(access, cancellationToken).ConfigureAwait(false);
            }
            else if (access != FolderAccess.None && folder.IsOpen && folder.Access < access)
            {
                // Need higher access level, close and reopen
                await folder.CloseAsync(false, cancellationToken).ConfigureAwait(false);
                await folder.OpenAsync(access, cancellationToken).ConfigureAwait(false);
            }

            return folder;
        }
        catch (FolderNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Error resolving folder '{name}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Converts a MailKit IMessageSummary to a ReceivedMessage
    /// </summary>
    private async Task<ReceivedMessage> ConvertToReceivedMessage(
        IMailFolder folder, IMessageSummary summary, bool includeBody,
        CancellationToken cancellationToken)
    {
        string? textBody = null;
        string? htmlBody = null;
        var attachments = new List<ReceivedAttachment>();

        if (includeBody && summary.UniqueId.IsValid)
        {
            try
            {
                var message = await folder.GetMessageAsync(summary.UniqueId, cancellationToken).ConfigureAwait(false);
                textBody = message.TextBody;
                htmlBody = message.HtmlBody;

                foreach (var attachment in message.Attachments)
                {
                    if (attachment is MimePart part)
                    {
                        attachments.Add(new ReceivedAttachment
                        {
                            FileName = part.FileName,
                            ContentType = part.ContentType.MimeType,
                            Size = 0 // Size not easily available from MimePart without downloading
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Could not fetch body for UID {summary.UniqueId}: {ex.Message}");
            }
        }

        var envelope = summary.Envelope;

        return new ReceivedMessage
        {
            MessageId = envelope.MessageId,
            Uid = summary.UniqueId.Id,
            From = envelope.From?.Mailboxes.FirstOrDefault()?.Address,
            FromName = envelope.From?.Mailboxes.FirstOrDefault()?.Name,
            To = envelope.To?.Mailboxes.Select(m => m.Address).ToList() ?? new List<string>(),
            Cc = envelope.Cc?.Mailboxes.Select(m => m.Address).ToList() ?? new List<string>(),
            Subject = envelope.Subject,
            TextBody = textBody,
            HtmlBody = htmlBody,
            Date = envelope.Date ?? DateTimeOffset.MinValue,
            Flags = ConvertFlags(summary.Flags ?? MkMessageFlags.None),
            Folder = folder.FullName,
            Attachments = attachments.AsReadOnly()
        };
    }

    /// <summary>
    /// Converts MailKit MessageFlags to our MessageFlags enum
    /// </summary>
    private static Models.MessageFlags ConvertFlags(MkMessageFlags mkFlags)
    {
        var flags = Models.MessageFlags.None;

        if (mkFlags.HasFlag(MkMessageFlags.Seen))
            flags |= Models.MessageFlags.Seen;
        if (mkFlags.HasFlag(MkMessageFlags.Flagged))
            flags |= Models.MessageFlags.Flagged;
        if (mkFlags.HasFlag(MkMessageFlags.Answered))
            flags |= Models.MessageFlags.Answered;
        if (mkFlags.HasFlag(MkMessageFlags.Deleted))
            flags |= Models.MessageFlags.Deleted;
        if (mkFlags.HasFlag(MkMessageFlags.Draft))
            flags |= Models.MessageFlags.Draft;

        return flags;
    }

    /// <summary>
    /// Converts our MessageFlags to MailKit MessageFlags
    /// </summary>
    private static MkMessageFlags ConvertToMailKitFlags(Models.MessageFlags flags)
    {
        var mkFlags = MkMessageFlags.None;

        if (flags.HasFlag(Models.MessageFlags.Seen))
            mkFlags |= MkMessageFlags.Seen;
        if (flags.HasFlag(Models.MessageFlags.Flagged))
            mkFlags |= MkMessageFlags.Flagged;
        if (flags.HasFlag(Models.MessageFlags.Answered))
            mkFlags |= MkMessageFlags.Answered;
        if (flags.HasFlag(Models.MessageFlags.Deleted))
            mkFlags |= MkMessageFlags.Deleted;
        if (flags.HasFlag(Models.MessageFlags.Draft))
            mkFlags |= MkMessageFlags.Draft;

        return mkFlags;
    }

    /// <summary>
    /// Builds a MailKit SearchQuery from ImapSearchCriteria
    /// </summary>
    private static SearchQuery BuildSearchQuery(ImapSearchCriteria criteria)
    {
        var query = criteria.IncludeDeleted ? SearchQuery.All : SearchQuery.NotDeleted;

        if (!string.IsNullOrWhiteSpace(criteria.Subject))
            query = query.And(SearchQuery.SubjectContains(criteria.Subject));

        if (!string.IsNullOrWhiteSpace(criteria.From))
            query = query.And(SearchQuery.FromContains(criteria.From));

        if (!string.IsNullOrWhiteSpace(criteria.To))
            query = query.And(SearchQuery.ToContains(criteria.To));

        if (!string.IsNullOrWhiteSpace(criteria.Body))
            query = query.And(SearchQuery.BodyContains(criteria.Body));

        if (criteria.Since.HasValue)
            query = query.And(SearchQuery.SentSince(criteria.Since.Value));

        if (criteria.Before.HasValue)
            query = query.And(SearchQuery.SentBefore(criteria.Before.Value));

        if (criteria.HasFlags.HasValue)
        {
            if (criteria.HasFlags.Value.HasFlag(Models.MessageFlags.Seen))
                query = query.And(SearchQuery.Seen);
            if (criteria.HasFlags.Value.HasFlag(Models.MessageFlags.Flagged))
                query = query.And(SearchQuery.Flagged);
            if (criteria.HasFlags.Value.HasFlag(Models.MessageFlags.Answered))
                query = query.And(SearchQuery.Answered);
            if (criteria.HasFlags.Value.HasFlag(Models.MessageFlags.Draft))
                query = query.And(SearchQuery.Draft);
        }

        if (criteria.NotFlags.HasValue)
        {
            if (criteria.NotFlags.Value.HasFlag(Models.MessageFlags.Seen))
                query = query.And(SearchQuery.NotSeen);
            if (criteria.NotFlags.Value.HasFlag(Models.MessageFlags.Flagged))
                query = query.And(SearchQuery.NotFlagged);
            if (criteria.NotFlags.Value.HasFlag(Models.MessageFlags.Answered))
                query = query.And(SearchQuery.NotAnswered);
            if (criteria.NotFlags.Value.HasFlag(Models.MessageFlags.Draft))
                query = query.And(SearchQuery.NotDraft);
        }

        return query;
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

    #endregion

    /// <summary>
    /// Disposes the IMAP client and stops IDLE
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopIdle();
        _client?.Dispose();
        _client = null;

        GC.SuppressFinalize(this);
    }
}
