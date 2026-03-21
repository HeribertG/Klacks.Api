// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace Klacks.Api.Infrastructure.Email;

public class ImapEmailService : IImapEmailService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IReceivedEmailRepository _receivedEmailRepository;
    private readonly IEmailFolderRepository _emailFolderRepository;
    private readonly ISettingsEncryptionService _encryptionService;
    private readonly ILogger<ImapEmailService> _logger;

    private record ImapSettings(string Server, int Port, string Username, string Password, SecureSocketOptions SocketOptions);

    public ImapEmailService(
        ISettingsRepository settingsRepository,
        IReceivedEmailRepository receivedEmailRepository,
        IEmailFolderRepository emailFolderRepository,
        ISettingsEncryptionService encryptionService,
        ILogger<ImapEmailService> logger)
    {
        _settingsRepository = settingsRepository;
        _receivedEmailRepository = receivedEmailRepository;
        _emailFolderRepository = emailFolderRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<List<ReceivedEmail>> FetchNewEmailsAsync(CancellationToken cancellationToken = default)
    {
        var folderNames = await GetFolderNamesAsync();

        try
        {
            return await WithImapClientAsync(async (client, ct) =>
            {
                var newEmails = new List<ReceivedEmail>();

                foreach (var folderName in folderNames)
                {
                    if (ct.IsCancellationRequested) break;

                    try
                    {
                        var fetchedEmails = await FetchEmailsFromFolderAsync(client, folderName, ct);
                        newEmails.AddRange(fetchedEmails);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch emails from folder {Folder}", folderName);
                    }
                }

                if (newEmails.Count > 0)
                {
                    _logger.LogInformation("Fetched {Count} new emails from IMAP server", newEmails.Count);
                }

                return newEmails;
            }, cancellationToken) ?? [];
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching emails from IMAP server");
            throw;
        }
    }

    public async Task SyncFoldersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await WithImapClientAsync(async (client, ct) =>
            {
                var personalNamespace = client.PersonalNamespaces[0];
                var imapFolders = await client.GetFoldersAsync(personalNamespace, cancellationToken: ct);
                var existingImapFolders = imapFolders.Where(f => f.Exists).ToList();
                var imapFolderNames = existingImapFolders
                    .Select(f => f.FullName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var dbFolders = await _emailFolderRepository.GetAllAsync();
                var dbFoldersByImapName = dbFolders.ToDictionary(f => f.ImapFolderName, StringComparer.OrdinalIgnoreCase);
                var appOnlyFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    EmailConstants.ClientAssignedFolder
                };

                var maxSortOrder = dbFolders.Count > 0 ? dbFolders.Max(f => f.SortOrder) : -1;

                foreach (var imapFolder in existingImapFolders)
                {
                    var folderName = imapFolder.FullName;
                    if (appOnlyFolders.Contains(folderName))
                        continue;

                    var specialUse = GetSpecialUse(imapFolder.Attributes);
                    var sortOrder = GetSpecialUseSortOrder(specialUse, ref maxSortOrder);

                    if (dbFoldersByImapName.TryGetValue(folderName, out var existingFolder))
                    {
                        if (specialUse != null && string.IsNullOrEmpty(existingFolder.SpecialUse))
                        {
                            await _emailFolderRepository.UpdateSpecialUseAsync(existingFolder.Id, specialUse);
                            await _emailFolderRepository.UpdateSortOrderAsync(existingFolder.Id, sortOrder);
                        }

                        if (!existingFolder.IsSystem)
                        {
                            await _emailFolderRepository.UpdateIsSystemAsync(existingFolder.Id, true);
                        }

                        continue;
                    }

                    await _emailFolderRepository.AddAsync(new EmailFolder
                    {
                        Name = folderName,
                        ImapFolderName = folderName,
                        SortOrder = sortOrder,
                        IsSystem = true,
                        SpecialUse = specialUse
                    });

                    _logger.LogInformation("Synced new IMAP folder: {Folder}", folderName);
                }

                await _emailFolderRepository.DeleteNonSystemByImapNamesAsync(imapFolderNames);
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing folders from IMAP server");
        }
    }

    public async Task SyncEmailStatesAsync(CancellationToken cancellationToken = default)
    {
        var folderNames = await GetFolderNamesAsync();
        var inboxName = await _emailFolderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);

        try
        {
            await WithImapClientAsync(async (client, ct) =>
            {
                foreach (var folderName in folderNames)
                {
                    if (ct.IsCancellationRequested) break;

                    try
                    {
                        await SyncFolderStatesAsync(client, folderName, inboxName, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to sync email states for folder {Folder}", folderName);
                    }
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing email states from IMAP server");
        }
    }

    public async Task MoveEmailOnImapAsync(long imapUid, string sourceFolder, string targetFolder, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedSource = await ResolveImapFolderNameAsync(sourceFolder);
            var resolvedTarget = await ResolveImapFolderNameAsync(targetFolder);

            if (string.Equals(resolvedSource, resolvedTarget, StringComparison.OrdinalIgnoreCase))
                return;

            await WithImapClientAsync(async (client, ct) =>
            {
                var source = await client.GetFolderAsync(resolvedSource, ct);
                await source.OpenAsync(FolderAccess.ReadWrite, ct);

                var target = await client.GetFolderAsync(resolvedTarget, ct);
                var uid = new UniqueId((uint)imapUid);

                await source.MoveToAsync(uid, target, ct);

                _logger.LogInformation("Moved email UID {Uid} from {Source} to {Target} on IMAP", imapUid, resolvedSource, resolvedTarget);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to move email UID {Uid} from {Source} to {Target} on IMAP", imapUid, sourceFolder, targetFolder);
        }
    }

    public async Task SetReadFlagOnImapAsync(long imapUid, string folder, bool isRead, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedFolder = await ResolveImapFolderNameAsync(folder);

            await WithImapClientAsync(async (client, ct) =>
            {
                var mailFolder = await client.GetFolderAsync(resolvedFolder, ct);
                await mailFolder.OpenAsync(FolderAccess.ReadWrite, ct);

                var uid = new UniqueId((uint)imapUid);

                if (isRead)
                {
                    await mailFolder.AddFlagsAsync(uid, MessageFlags.Seen, true, ct);
                }
                else
                {
                    await mailFolder.RemoveFlagsAsync(uid, MessageFlags.Seen, true, ct);
                }

                _logger.LogDebug("Set read flag to {IsRead} for email UID {Uid} in {Folder} on IMAP", isRead, imapUid, resolvedFolder);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set read flag for email UID {Uid} in {Folder} on IMAP", imapUid, folder);
        }
    }

    public async Task DeleteEmailOnImapAsync(long imapUid, string folder, CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedFolder = await ResolveImapFolderNameAsync(folder);

            await WithImapClientAsync(async (client, ct) =>
            {
                var mailFolder = await client.GetFolderAsync(resolvedFolder, ct);
                await mailFolder.OpenAsync(FolderAccess.ReadWrite, ct);

                var uid = new UniqueId((uint)imapUid);

                await mailFolder.AddFlagsAsync(uid, MessageFlags.Deleted, true, ct);
                await mailFolder.ExpungeAsync([uid], ct);

                _logger.LogInformation("Permanently deleted email UID {Uid} from {Folder} on IMAP", imapUid, resolvedFolder);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete email UID {Uid} from {Folder} on IMAP", imapUid, folder);
        }
    }

    public async Task CreateFolderOnImapAsync(string folderName, CancellationToken cancellationToken = default)
    {
        try
        {
            await WithImapClientAsync(async (client, ct) =>
            {
                var toplevel = client.GetFolder(client.PersonalNamespaces[0]);
                await toplevel.CreateAsync(folderName, true, ct);

                _logger.LogInformation("Created folder {Folder} on IMAP server", folderName);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create folder {Folder} on IMAP server", folderName);
        }
    }

    public async Task DeleteFolderOnImapAsync(string folderName, CancellationToken cancellationToken = default)
    {
        try
        {
            await WithImapClientAsync(async (client, ct) =>
            {
                var folder = await client.GetFolderAsync(folderName, ct);
                await folder.DeleteAsync(ct);

                _logger.LogInformation("Deleted folder {Folder} on IMAP server", folderName);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete folder {Folder} on IMAP server", folderName);
        }
    }

    private async Task SyncFolderStatesAsync(ImapClient client, string folderName, string? inboxName, CancellationToken cancellationToken)
    {
        var mailFolder = await client.GetFolderAsync(folderName, cancellationToken);
        await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var summaries = await mailFolder.FetchAsync(0, -1,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Flags, cancellationToken);

        var imapUids = summaries.ToDictionary(
            s => (long)s.UniqueId.Id,
            s => s.Flags ?? MessageFlags.None);

        var dbEmails = await _receivedEmailRepository.GetListByFolderAsync(folderName, 0, 10_000);

        var isInboxFolder = !string.IsNullOrEmpty(inboxName)
            && string.Equals(folderName, inboxName, StringComparison.OrdinalIgnoreCase);

        if (isInboxFolder)
        {
            var clientAssigned = await _receivedEmailRepository.GetListByFolderAsync(
                EmailConstants.ClientAssignedFolder, 0, 10_000);
            dbEmails.AddRange(clientAssigned);
        }

        var deletedCount = 0;
        var readStateChanges = 0;

        foreach (var dbEmail in dbEmails)
        {
            if (!imapUids.TryGetValue(dbEmail.ImapUid, out var flags))
            {
                await _receivedEmailRepository.DeleteAsync(dbEmail.Id);
                deletedCount++;
                continue;
            }

            var imapIsRead = flags.HasFlag(MessageFlags.Seen);
            if (dbEmail.IsRead != imapIsRead)
            {
                var tracked = await _receivedEmailRepository.GetByIdAsync(dbEmail.Id);
                if (tracked != null)
                {
                    tracked.IsRead = imapIsRead;
                    await _receivedEmailRepository.UpdateAsync(tracked);
                    readStateChanges++;
                }
            }
        }

        if (deletedCount > 0 || readStateChanges > 0)
        {
            _logger.LogInformation(
                "Synced folder {Folder}: {Deleted} removed, {ReadChanges} read state changes",
                folderName, deletedCount, readStateChanges);
        }
    }

    private async Task<ImapSettings?> GetImapSettingsAsync()
    {
        var server = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER);
        var portStr = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_PORT);
        var username = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_USERNAME);
        var password = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_PASSWORD);
        var sslStr = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_SSL);

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        if (!int.TryParse(portStr, out var port))
            port = 993;

        var enableSsl = string.Equals(sslStr, "true", StringComparison.OrdinalIgnoreCase);
        password = _encryptionService.ProcessForReading(Settings.APP_INCOMING_SERVER_PASSWORD, password);

        var socketOptions = GetSecureSocketOptions(port, enableSsl);

        return new ImapSettings(server, port, username, password, socketOptions);
    }

    private static async Task ConnectClientAsync(ImapClient client, ImapSettings settings, CancellationToken cancellationToken)
    {
        await client.ConnectAsync(settings.Server, settings.Port, settings.SocketOptions, cancellationToken);
        await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
    }

    private async Task WithImapClientAsync(Func<ImapClient, CancellationToken, Task> operation, CancellationToken ct)
    {
        var settings = await GetImapSettingsAsync();
        if (settings == null) return;

        using var client = new ImapClient();
        await ConnectClientAsync(client, settings, ct);
        try { await operation(client, ct); }
        finally { await client.DisconnectAsync(true, ct); }
    }

    private async Task<T?> WithImapClientAsync<T>(Func<ImapClient, CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        var settings = await GetImapSettingsAsync();
        if (settings == null) return default;

        using var client = new ImapClient();
        await ConnectClientAsync(client, settings, ct);
        try { return await operation(client, ct); }
        finally { await client.DisconnectAsync(true, ct); }
    }

    private async Task<string> ResolveImapFolderNameAsync(string dbFolder)
    {
        if (string.Equals(dbFolder, EmailConstants.ClientAssignedFolder, StringComparison.OrdinalIgnoreCase))
        {
            var inboxName = await _emailFolderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
            return inboxName ?? "INBOX";
        }

        return dbFolder;
    }

    private static string? GetSpecialUse(FolderAttributes attributes)
    {
        if (attributes.HasFlag(FolderAttributes.Inbox)) return FolderSpecialUse.Inbox;
        if (attributes.HasFlag(FolderAttributes.Trash)) return FolderSpecialUse.Trash;
        if (attributes.HasFlag(FolderAttributes.Junk)) return FolderSpecialUse.Junk;
        if (attributes.HasFlag(FolderAttributes.Sent)) return FolderSpecialUse.Sent;
        return null;
    }

    private static int GetSpecialUseSortOrder(string? specialUse, ref int maxSortOrder)
    {
        if (specialUse == FolderSpecialUse.Inbox) return 0;
        if (specialUse == FolderSpecialUse.Sent) return 100;
        if (specialUse == FolderSpecialUse.Junk) return 900;
        if (specialUse == FolderSpecialUse.Trash) return 999;

        maxSortOrder++;
        return maxSortOrder;
    }

    private async Task<List<string>> GetFolderNamesAsync()
    {
        var dbFolders = await _emailFolderRepository.GetAllAsync();
        var appOnlyFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            EmailConstants.ClientAssignedFolder
        };
        var imapFolders = dbFolders
            .Where(f => !appOnlyFolders.Contains(f.ImapFolderName))
            .Select(f => f.ImapFolderName)
            .ToList();

        if (imapFolders.Count > 0)
        {
            return imapFolders;
        }

        var settingsFolder = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_FOLDER);
        return [string.IsNullOrWhiteSpace(settingsFolder) ? "INBOX" : settingsFolder];
    }

    private async Task<List<ReceivedEmail>> FetchEmailsFromFolderAsync(ImapClient client, string folder, CancellationToken cancellationToken)
    {
        var newEmails = new List<ReceivedEmail>();

        var mailFolder = await client.GetFolderAsync(folder, cancellationToken);
        await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var highestUid = await _receivedEmailRepository.GetHighestImapUidAsync(folder);
        var query = highestUid > 0
            ? SearchQuery.Uids(new UniqueIdRange(new UniqueId((uint)highestUid + 1), UniqueId.MaxValue))
            : SearchQuery.All;

        var uids = await mailFolder.SearchAsync(query, cancellationToken);

        _logger.LogInformation("Found {Count} messages to process in folder {Folder}", uids.Count, folder);

        foreach (var uid in uids)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var message = await mailFolder.GetMessageAsync(uid, cancellationToken);
                var messageId = message.MessageId ?? $"{folder}-{uid.Id}";

                if (await _receivedEmailRepository.ExistsByMessageIdAsync(messageId))
                {
                    continue;
                }

                var receivedEmail = MapToReceivedEmail(message, uid, folder, messageId);
                await _receivedEmailRepository.AddAsync(receivedEmail);
                newEmails.Add(receivedEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process message UID {Uid} in folder {Folder}", uid.Id, folder);
            }
        }

        return newEmails;
    }

    private static ReceivedEmail MapToReceivedEmail(MimeMessage message, UniqueId uid, string folder, string messageId)
    {
        var from = message.From.Mailboxes.FirstOrDefault();

        return new ReceivedEmail
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            ImapUid = uid.Id,
            Folder = folder,
            SourceImapFolder = folder,
            FromAddress = from?.Address ?? string.Empty,
            FromName = from?.Name,
            ToAddress = string.Join(", ", message.To.Mailboxes.Select(m => m.Address)),
            Subject = message.Subject ?? string.Empty,
            BodyHtml = message.HtmlBody,
            BodyText = message.TextBody,
            ReceivedDate = message.Date.UtcDateTime,
            IsRead = false,
            HasAttachments = message.Attachments.Any()
        };
    }

    private static SecureSocketOptions GetSecureSocketOptions(int port, bool enableSsl)
    {
        if (port == 993)
        {
            return SecureSocketOptions.SslOnConnect;
        }

        if (port == 143)
        {
            return enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        }

        return enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
    }

    private async Task<string> GetSettingValueAsync(string type)
    {
        var setting = await _settingsRepository.GetSetting(type);
        return setting?.Value ?? string.Empty;
    }
}
