// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using IEmailNotificationService = Klacks.Api.Domain.Interfaces.Email.IEmailNotificationService;

namespace Klacks.Api.Infrastructure.Email;

public class EmailPollingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailPollingBackgroundService> _logger;

    private const int DefaultIntervalSeconds = 300;

    public EmailPollingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailPollingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email polling background service started");

        await InitialSyncAsync(stoppingToken);
        await ReclassifyExistingEmailsAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var intervalSeconds = DefaultIntervalSeconds;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

                    var serverSetting = await settingsRepository.GetSetting(Settings.APP_INCOMING_SERVER);
                    if (string.IsNullOrWhiteSpace(serverSetting?.Value))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
                        continue;
                    }

                    var intervalSetting = await settingsRepository.GetSetting(Settings.APP_INCOMING_SERVER_POLL_INTERVAL);
                    if (int.TryParse(intervalSetting?.Value, out var configuredInterval) && configuredInterval > 0)
                    {
                        intervalSeconds = configuredInterval;
                    }

                    var emailService = scope.ServiceProvider.GetRequiredService<IImapEmailService>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    await emailService.SyncFoldersAsync(stoppingToken);
                    await unitOfWork.CompleteAsync();

                    var newEmails = await emailService.FetchNewEmailsAsync(stoppingToken);

                    if (newEmails.Count > 0)
                    {
                        var spamFilterService = scope.ServiceProvider.GetRequiredService<ISpamFilterService>();
                        var assignmentService = scope.ServiceProvider.GetRequiredService<IEmailClientAssignmentService>();
                        var folderRepository = scope.ServiceProvider.GetRequiredService<IEmailFolderRepository>();

                        var inboxFolder = await folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
                        var junkFolder = await folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Junk);

                        if (!string.IsNullOrEmpty(inboxFolder) && !string.IsNullOrEmpty(junkFolder))
                        {
                            foreach (var email in newEmails.Where(e => string.Equals(e.Folder, inboxFolder, StringComparison.OrdinalIgnoreCase)))
                            {
                                var spamResult = await spamFilterService.ClassifyAsync(email, stoppingToken);
                                if (spamResult.IsSpam)
                                {
                                    email.Folder = junkFolder;
                                    await emailService.MoveEmailOnImapAsync(email.ImapUid, inboxFolder, junkFolder, stoppingToken);
                                    _logger.LogInformation("Email from {From} classified as spam: {Reason}", email.FromAddress, spamResult.Reason);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(inboxFolder))
                        {
                            foreach (var email in newEmails.Where(e => string.Equals(e.Folder, inboxFolder, StringComparison.OrdinalIgnoreCase)))
                            {
                                await assignmentService.AssignNewEmailAsync(email);
                            }
                        }

                        await unitOfWork.CompleteAsync();
                        _logger.LogInformation("Saved {Count} new emails to database", newEmails.Count);

                        var notificationService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
                        await notificationService.NotifyNewEmailsAsync(newEmails.Count);
                    }

                    await emailService.SyncEmailStatesAsync(stoppingToken);
                    await unitOfWork.CompleteAsync();
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in email polling background service");
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Email polling background service stopped");
    }

    private async Task InitialSyncAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IImapEmailService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await emailService.SyncFoldersAsync(stoppingToken);
            await unitOfWork.CompleteAsync();
            _logger.LogInformation("Initial folder sync completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Initial folder sync failed");
        }
    }

    private async Task ReclassifyExistingEmailsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var emailRepository = scope.ServiceProvider.GetRequiredService<IReceivedEmailRepository>();
            var folderRepository = scope.ServiceProvider.GetRequiredService<IEmailFolderRepository>();
            var spamFilterService = scope.ServiceProvider.GetRequiredService<ISpamFilterService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IImapEmailService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var inboxFolder = await folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
            var junkFolder = await folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Junk);

            if (!string.IsNullOrEmpty(inboxFolder) && !string.IsNullOrEmpty(junkFolder))
            {
                var inboxEmails = await emailRepository.GetListByFolderAsync(inboxFolder, 0, int.MaxValue);
                var clientAssignedEmails = await emailRepository.GetListByFolderAsync(EmailConstants.ClientAssignedFolder, 0, int.MaxValue);
                var movedCount = 0;

                foreach (var email in inboxEmails)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    var result = await spamFilterService.ClassifyAsync(email, stoppingToken);
                    if (result.IsSpam)
                    {
                        await emailRepository.MoveToFolderAsync(email.Id, junkFolder);
                        await emailService.MoveEmailOnImapAsync(email.ImapUid, inboxFolder, junkFolder, stoppingToken);
                        movedCount++;
                    }
                }

                foreach (var email in clientAssignedEmails)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    var result = await spamFilterService.ClassifyAsync(email, stoppingToken);
                    if (result.IsSpam)
                    {
                        await emailRepository.MoveToFolderAsync(email.Id, junkFolder);
                        await emailService.MoveEmailOnImapAsync(email.ImapUid, email.SourceImapFolder, junkFolder, stoppingToken);
                        movedCount++;
                    }
                }

                if (movedCount > 0)
                {
                    await unitOfWork.CompleteAsync();
                    _logger.LogInformation("Reclassified {Count} existing emails as spam", movedCount);
                }
            }

            var assignmentService = scope.ServiceProvider.GetRequiredService<IEmailClientAssignmentService>();
            await assignmentService.AssignInboxEmailsToClientsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reclassifying existing emails");
        }
    }
}
