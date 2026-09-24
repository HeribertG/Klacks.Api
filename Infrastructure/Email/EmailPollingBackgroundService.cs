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

                        await ProcessBatchAsync(toProcess, inboxFolder, junkFolder, stoppingToken);
                    }

                    using (var syncScope = _scopeFactory.CreateScope())
                    {
                        var syncEmailService = syncScope.ServiceProvider.GetRequiredService<IImapEmailService>();
                        var syncUnitOfWork = syncScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        await syncEmailService.SyncEmailStatesAsync(stoppingToken);
                        await syncUnitOfWork.CompleteAsync();
                    }

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
    /// Runs ProcessEmailAsync for every mail of one poll cycle, each in its OWN DI scope (and therefore
    /// its own DataBaseContext/change tracker and its own IUnitOfWork), never the scope that fetched
    /// toProcess. A single shared scope for the whole batch would let one mail's failed SaveChanges (an
    /// entity stuck in the tracker, e.g. a duplicate-key insert) poison every later commit in the same
    /// cycle, and a transient failure could let the next mail's own commit flush the previous mail's
    /// still-staged, unrelated changes along with it. The mail handed to ProcessEmailAsync is reloaded
    /// inside the new scope (GetByIdAsync) rather than reusing the instance tracked by the outer scope,
    /// so writes to Folder/ProcessedAt land in the same context as that mail's own CompleteAsync calls;
    /// a mail that no longer exists (deleted between the fetch and this loop) is skipped. For the same
    /// reason, ExecuteAsync must not run any further tracked ReceivedEmail work on the fetching scope
    /// afterwards (e.g. SyncEmailStatesAsync belongs in its own fresh scope): the fetching scope's
    /// change tracker still holds toProcess with its pre-processing values, and EF's identity resolution
    /// would hand those stale instances back to a tracked query for the same Ids instead of the current
    /// row. A per-mail failure before ProcessEmailAsync's own try/catch (CreateScope, service resolution,
    /// GetByIdAsync) is caught here so it only skips that one mail.
    /// </summary>
    internal async Task ProcessBatchAsync(
        IReadOnlyList<ReceivedEmail> toProcess, string inboxFolder, string junkFolder, CancellationToken stoppingToken)
    {
        foreach (var email in toProcess)
        {
            try
            {
                using var mailScope = _scopeFactory.CreateScope();
                var mailUnitOfWork = mailScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var mailEmail = await mailScope.ServiceProvider.GetRequiredService<IReceivedEmailRepository>()
                    .GetByIdAsync(email.Id);
                if (mailEmail == null)
                {
                    _logger.LogInformation("Mail {EmailId} vanished before processing; skipped", email.Id);
                    continue;
                }

                await ProcessEmailAsync(mailScope, mailUnitOfWork, mailEmail, inboxFolder, junkFolder, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Setting up per-mail processing failed for email {EmailId}, will retry next cycle", email.Id);
            }
        }
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
    /// Called once per mail from ProcessBatchAsync with a fresh per-mail scope, and directly by tests.
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

            var source = ToInboundSource(email);

            (Guid ClientId, EntityTypeEnum ClientType)? client = null;
            if (emailAnalysisEnabled)
            {
                var alreadyAnalyzed = await scope.ServiceProvider.GetRequiredService<IInboundAnalysisRepository>()
                    .ExistsBySourceAsync(InboundSourceKind.Email, email.Id, stoppingToken);
                if (alreadyAnalyzed)
                {
                    _logger.LogWarning(
                        "message {SourceId} already analyzed — skipped; hard-delete the inbound_analyses row to reprocess", email.Id);
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

                clarificationContext = ClarificationContextBlocks.Join(clarificationContext, postAnalysis.NotifierContext);
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
