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
        var server = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER);
        var portStr = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_PORT);
        var username = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_USERNAME);
        var password = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_PASSWORD);
        var sslStr = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_SSL);

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogDebug("IMAP settings not configured, skipping email fetch");
            return [];
        }

        if (!int.TryParse(portStr, out var port))
        {
            port = 993;
        }

        var enableSsl = string.Equals(sslStr, "true", StringComparison.OrdinalIgnoreCase);
        password = _encryptionService.ProcessForReading(Settings.APP_INCOMING_SERVER_PASSWORD, password);

        var secureSocketOptions = GetSecureSocketOptions(port, enableSsl);

        var folderNames = await GetFolderNamesAsync();
        var newEmails = new List<ReceivedEmail>();

        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(server, port, secureSocketOptions, cancellationToken);
            await client.AuthenticateAsync(username, password, cancellationToken);

            foreach (var folderName in folderNames)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var fetchedEmails = await FetchEmailsFromFolderAsync(client, folderName, cancellationToken);
                    newEmails.AddRange(fetchedEmails);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch emails from folder {Folder}", folderName);
                }
            }

            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching emails from IMAP server {Server}:{Port}", server, port);
            throw;
        }

        if (newEmails.Count > 0)
        {
            _logger.LogInformation("Fetched {Count} new emails from {Server}", newEmails.Count, server);
        }

        return newEmails;
    }

    public async Task SyncFoldersAsync(CancellationToken cancellationToken = default)
    {
        var server = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER);
        var portStr = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_PORT);
        var username = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_USERNAME);
        var password = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_PASSWORD);
        var sslStr = await GetSettingValueAsync(Settings.APP_INCOMING_SERVER_SSL);

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return;

        if (!int.TryParse(portStr, out var port))
            port = 993;

        var enableSsl = string.Equals(sslStr, "true", StringComparison.OrdinalIgnoreCase);
        password = _encryptionService.ProcessForReading(Settings.APP_INCOMING_SERVER_PASSWORD, password);
        var secureSocketOptions = GetSecureSocketOptions(port, enableSsl);

        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(server, port, secureSocketOptions, cancellationToken);
            await client.AuthenticateAsync(username, password, cancellationToken);

            var personalNamespace = client.PersonalNamespaces[0];
            var imapFolders = await client.GetFoldersAsync(personalNamespace, cancellationToken: cancellationToken);
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

            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing folders from IMAP server {Server}:{Port}", server, port);
        }
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
        var excludedSpecialUses = new HashSet<string> { FolderSpecialUse.Trash, FolderSpecialUse.Junk };
        var imapFolders = dbFolders
            .Where(f => !excludedSpecialUses.Contains(f.SpecialUse ?? string.Empty))
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
