// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Infrastructure.Inbound;
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
    /// safe. An email whose analysis already exists (e.g. processed_at was reset by hand) is only marked
    /// processed, before the client lookup and the LLM call; otherwise the unique source index would fail
    /// the insert on every cycle. Before the analysis it commits the staged client-assignment changes (the
    /// clarification repository is self-committing and must not flush them implicitly) and asks
    /// IClarificationCoordinator whether the mail answers an open clarification: that answer analysis then
    /// replaces the regular one and is persisted as the very instance (and Id) the clarification already
    /// references. After persisting and committing the analysis together with ProcessedAt it lets the
    /// coordinator ask back once, handing on the in-memory analysis; when a question went out, the action
    /// orchestrator and the regular notification are skipped. Both hooks run through
    /// ClarificationDialogSafeGuard, so a failing clarification dialog degrades to the regular path.
    /// </summary>
    internal async Task ProcessEmailAsync(
        IServiceScope scope,
        IUnitOfWork unitOfWork,
        ReceivedEmail email,
        string inboxFolder,
        string junkFolder,
        CancellationToken stoppingToken)
    {
        try
        {
            var assignmentService = scope.ServiceProvider.GetRequiredService<IEmailClientAssignmentService>();

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
                    await assignmentService.AssignNewEmailAsync(email);
                }
            }

            if (string.Equals(email.Folder, junkFolder, StringComparison.OrdinalIgnoreCase))
            {
                email.ProcessedAt = DateTime.UtcNow;
                await unitOfWork.CompleteAsync();
                return;
            }

            var settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

            var emailAnalysisSetting = await settingsRepository.GetSetting(Settings.EMAIL_ANALYSIS_ENABLED);
            var emailAnalysisEnabled = emailAnalysisSetting?.Value != null && bool.TryParse(emailAnalysisSetting.Value, out var enabled) && enabled;

            // Built unconditionally (pure mapping over email, no side effects) so it is available both
            // to the intent analysis below and to the action orchestrator further down, which needs the
            // same InboundSource for its audit trail.
            var source = ToInboundSource(email);

            (Guid ClientId, EntityTypeEnum ClientType)? client = null;
            if (emailAnalysisEnabled)
            {
                var existingAnalysis = await scope.ServiceProvider.GetRequiredService<IInboundAnalysisRepository>()
                    .GetBySourceAsync(InboundSourceKind.Email, email.Id, stoppingToken);
                if (existingAnalysis != null)
                {
                    _logger.LogInformation(
                        "Email {EmailId} was already analyzed; marking it processed without a new analysis", email.Id);
                    email.ProcessedAt = DateTime.UtcNow;
                    await unitOfWork.CompleteAsync();
                    return;
                }

                client = await assignmentService.ResolveClientAsync(email, stoppingToken);
            }

            if (client == null)
            {
                email.ProcessedAt = DateTime.UtcNow;
                await unitOfWork.CompleteAsync();
                return;
            }

            var (clientId, clientType) = client.Value;
            await unitOfWork.CompleteAsync();

            var clarificationRequest = ToClarificationRequest(email, clientId, clientType, source);
            var preAnalysis = await ClarificationDialogSafeGuard.BeforeAnalysisSafelyAsync(
                scope.ServiceProvider, clarificationRequest, _logger, stoppingToken);

            var analysis = preAnalysis.AnswerAnalysis
                ?? await scope.ServiceProvider.GetRequiredService<IInboundIntentAnalysisService>()
                    .AnalyzeAsync(clientId, clientType, source, stoppingToken);

            email.ProcessedAt = DateTime.UtcNow;

            var analysisRepository = scope.ServiceProvider.GetRequiredService<IInboundAnalysisRepository>();
            await analysisRepository.AddAsync(analysis, stoppingToken);
            await unitOfWork.CompleteAsync();

            var clarificationContext = preAnalysis.NotifierContext;
            if (preAnalysis.AnswerAnalysis == null)
            {
                var postAnalysis = await ClarificationDialogSafeGuard.AfterAnalysisSafelyAsync(
                    scope.ServiceProvider, clarificationRequest, analysis, _logger, stoppingToken);
                if (postAnalysis.QuestionSent)
                {
                    return;
                }

                clarificationContext = ClarificationNotificationTexts.JoinContext(clarificationContext, postAnalysis.NotifierContext);
            }

            var actionOrchestrator = scope.ServiceProvider.GetRequiredService<IInboundActionOrchestrator>();
            var resolvedClientId = analysis.ClientId ?? clientId;
            var actionOutcome = await actionOrchestrator.ExecuteAsync(resolvedClientId, source, analysis, stoppingToken);

            string? periodLoadSummary = null;
            if (analysis.FromDate != null && analysis.ClientType != EntityTypeEnum.Customer)
            {
                var periodLoadService = scope.ServiceProvider.GetRequiredService<IEmailPeriodLoadService>();
                periodLoadSummary = await periodLoadService.BuildSummaryAsync(
                    resolvedClientId, analysis.FromDate.Value,
                    analysis.UntilDate ?? analysis.FromDate.Value, stoppingToken);
            }

            var analysisNotifier = scope.ServiceProvider.GetRequiredService<IInboundAnalysisNotifier>();
            await analysisNotifier.NotifyAsync(source, analysis, actionOutcome, periodLoadSummary, clarificationContext, stoppingToken);
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

    private static InboundSource ToInboundSource(ReceivedEmail email) => new(
        email.Id, InboundSourceKind.Email, EmailConstants.InboundChannel,
        string.IsNullOrWhiteSpace(email.FromName) ? email.FromAddress : $"{email.FromName} ({email.FromAddress})",
        email.Subject, email.BodyText ?? email.BodyHtml ?? string.Empty, email.ReceivedDate);

    private static ClarificationRequest ToClarificationRequest(
        ReceivedEmail email, Guid clientId, EntityTypeEnum clientType, InboundSource source) => new(
        ClientId: clientId,
        ClientType: clientType,
        Source: source,
        ReplyChannel: EmailConstants.InboundChannel,
        SenderAddress: email.FromAddress,
        EmailThread: new ClarificationEmailThread(
            MessageId: email.MessageId,
            InReplyTo: email.InReplyTo,
            ThreadReferences: email.ThreadReferences,
            IsAutoGenerated: email.IsAutoGenerated));

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

    internal async Task<int> ClassifyFolderBatchedAsync(
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
