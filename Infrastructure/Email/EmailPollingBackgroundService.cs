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
    private const int UnprocessedBatchSize = 200;

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
                        await unitOfWork.CompleteAsync();
                        _logger.LogInformation("Saved {Count} new emails to database", newEmails.Count);

                        var notificationService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
                        await notificationService.NotifyNewEmailsAsync(newEmails.Count);
                    }

                    var folderRepository = scope.ServiceProvider.GetRequiredService<IEmailFolderRepository>();
                    var inboxFolder = await folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
                    var junkFolder = await folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Junk);

                    if (!string.IsNullOrEmpty(inboxFolder) && !string.IsNullOrEmpty(junkFolder))
                    {
                        var receivedEmailRepository = scope.ServiceProvider.GetRequiredService<IReceivedEmailRepository>();
                        var toProcess = await receivedEmailRepository.GetUnprocessedAsync(UnprocessedBatchSize);

                        if (toProcess.Count > newEmails.Count)
                        {
                            _logger.LogInformation(
                                "Processing {Total} received emails ({Backlog} carried over unprocessed from a previous cycle)",
                                toProcess.Count, toProcess.Count - newEmails.Count);
                        }

                        foreach (var email in toProcess)
                        {
                            await ProcessEmailAsync(scope, unitOfWork, email, inboxFolder, junkFolder, stoppingToken);
                        }
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

    /// <summary>
    /// Runs the full per-email pipeline (spam-classify, client assignment, intent analysis) and marks
    /// ProcessedAt only on definitive completion — an unhandled exception leaves ProcessedAt null so
    /// GetUnprocessedAsync retries this email on the next poll cycle instead of dropping it silently.
    /// Idempotent by design: re-running on an email already past a given stage (e.g. already moved out
    /// of the inbox folder) just skips that stage, which is what makes retry-from-any-interruption-point
    /// safe.
    /// </summary>
    private async Task ProcessEmailAsync(
        IServiceScope scope,
        IUnitOfWork unitOfWork,
        Domain.Models.Email.ReceivedEmail email,
        string inboxFolder,
        string junkFolder,
        CancellationToken stoppingToken)
    {
        try
        {
            if (string.Equals(email.Folder, inboxFolder, StringComparison.OrdinalIgnoreCase))
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IImapEmailService>();
                var spamFilterService = scope.ServiceProvider.GetRequiredService<ISpamFilterService>();

                var spamResult = await spamFilterService.ClassifyAsync(email, stoppingToken);
                if (spamResult.IsSpam)
                {
                    email.Folder = junkFolder;
                    await emailService.MoveEmailOnImapAsync(email.ImapUid, inboxFolder, junkFolder, stoppingToken);
                    _logger.LogInformation("Email from {From} classified as spam: {Reason}", email.FromAddress, spamResult.Reason);
                }
                else
                {
                    var assignmentService = scope.ServiceProvider.GetRequiredService<IEmailClientAssignmentService>();
                    await assignmentService.AssignNewEmailAsync(email);
                }
            }

            if (string.Equals(email.Folder, junkFolder, StringComparison.OrdinalIgnoreCase))
            {
                email.ProcessedAt = DateTime.UtcNow;
                await unitOfWork.CompleteAsync();
                return;
            }

            var analysisService = scope.ServiceProvider.GetRequiredService<IEmailIntentAnalysisService>();
            var analysis = await analysisService.AnalyzeAsync(email, stoppingToken);
            email.ProcessedAt = DateTime.UtcNow;

            if (analysis == null)
            {
                await unitOfWork.CompleteAsync();
                return;
            }

            var analysisRepository = scope.ServiceProvider.GetRequiredService<IEmailAnalysisRepository>();
            await analysisRepository.AddAsync(analysis, stoppingToken);
            await unitOfWork.CompleteAsync();

            var actionOrchestrator = scope.ServiceProvider.GetRequiredService<IEmailActionOrchestrator>();
            var actionOutcome = await actionOrchestrator.ExecuteAsync(email, analysis, stoppingToken);

            string? periodLoadSummary = null;
            if (analysis.ClientId != null && analysis.FromDate != null
                && analysis.ClientType != Domain.Enums.EntityTypeEnum.Customer)
            {
                var periodLoadService = scope.ServiceProvider.GetRequiredService<IEmailPeriodLoadService>();
                periodLoadSummary = await periodLoadService.BuildSummaryAsync(
                    analysis.ClientId.Value, analysis.FromDate.Value,
                    analysis.UntilDate ?? analysis.FromDate.Value, stoppingToken);
            }

            var analysisNotifier = scope.ServiceProvider.GetRequiredService<IEmailAnalysisNotifier>();
            await analysisNotifier.NotifyAsync(email, analysis, actionOutcome, periodLoadSummary, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Email processing failed for email {EmailId} from {From}, will retry next cycle",
                email.Id, email.FromAddress);
        }
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
                var movedCount = 0;
                movedCount += await ClassifyFolderBatchedAsync(emailRepository, spamFilterService, emailService,
                    inboxFolder, junkFolder, stoppingToken);
                movedCount += await ClassifyFolderBatchedAsync(emailRepository, spamFilterService, emailService,
                    EmailConstants.ClientAssignedFolder, junkFolder, stoppingToken);

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

    private async Task<int> ClassifyFolderBatchedAsync(
        IReceivedEmailRepository emailRepository,
        ISpamFilterService spamFilterService,
        IImapEmailService emailService,
        string sourceFolder,
        string junkFolder,
        CancellationToken stoppingToken)
    {
        const int batchSize = 100;
        var movedCount = 0;
        var skip = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var emails = await emailRepository.GetListByFolderAsync(sourceFolder, skip, batchSize);
            if (emails.Count == 0) break;

            foreach (var email in emails)
            {
                if (stoppingToken.IsCancellationRequested) break;

                var result = await spamFilterService.ClassifyAsync(email, stoppingToken);
                if (result.IsSpam)
                {
                    var imapSource = string.IsNullOrEmpty(email.SourceImapFolder) ? sourceFolder : email.SourceImapFolder;
                    await emailRepository.MoveToFolderAsync(email.Id, junkFolder);
                    await emailService.MoveEmailOnImapAsync(email.ImapUid, imapSource, junkFolder, stoppingToken);
                    movedCount++;
                }
            }

            if (emails.Count < batchSize) break;
            skip += batchSize;
        }

        return movedCount;
    }
}
