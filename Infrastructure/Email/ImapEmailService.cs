// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
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

    private async Task<List<string>> GetFolderNamesAsync()
    {
        var dbFolders = await _emailFolderRepository.GetAllAsync();
        if (dbFolders.Count > 0)
        {
            return dbFolders.Select(f => f.ImapFolderName).ToList();
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
