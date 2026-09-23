// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs one queued messenger message resolved to a known CLIENT (employee, extern employee, customer)
/// through the same channel-neutral intent-analysis/action-orchestration/notification kernel the email
/// pipeline uses, mirroring EmailPollingBackgroundService.ProcessEmailAsync step for step: feature gate
/// MESSENGER_ANALYSIS_ENABLED, client-type resolution, analyze, persist + commit, execute actions,
/// period-load digest (employees/externs with a dated intent only), notify planners/admins.
/// Around the analysis it runs the inbound clarification dialog (IClarificationCoordinator, resolved lazily
/// like the other kernel services): an answer to an open clarification replaces the regular analysis and
/// is persisted and acted on exactly as returned (same instance and Id, which the clarification already
/// references), and when a clarification question went out the action orchestrator and the regular
/// notification are skipped for the unclear message. The regular analysis is persisted and committed
/// before the post-analysis check, so the clarification can reference it; the in-memory instance is
/// handed on because some flags are not persisted. A message whose analysis already exists is skipped
/// before the clarification check and the LLM call (idempotency against a re-delivered message). A
/// failure of the clarification dialog never breaks the adapter: it degrades to the regular path.
/// The sender shown to planners is the client's own name from the database (the messenger profile name
/// is chosen by the user and may be a nickname), falling back to SenderDisplayName, then Sender.
/// The kernel services are resolved lazily from the (per-message) scope instead of the constructor on
/// purpose: MESSENGER_ANALYSIS_ENABLED is off by default, and constructor injection would build the
/// whole analysis/action/notification graph (LLM provider resolution, orchestrator, notifier) for every
/// inbound message even when the feature is off; IEmailPeriodLoadService is likewise only resolved in
/// the branch that needs it, exactly as in the email adapter.
/// </summary>
/// <param name="serviceProvider">Service provider of the per-message scope created by MessengerIntentBackgroundService</param>
/// <param name="logger">Structured log of skip decisions</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Infrastructure.Inbound;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public sealed class MessengerIntentProcessor : IMessengerIntentProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessengerIntentProcessor> _logger;

    public MessengerIntentProcessor(IServiceProvider serviceProvider, ILogger<MessengerIntentProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProcessAsync(InboundClientMessengerMessage message, CancellationToken cancellationToken)
    {
        var settingsRepository = _serviceProvider.GetRequiredService<ISettingsRepository>();
        var setting = await settingsRepository.GetSetting(Settings.MESSENGER_ANALYSIS_ENABLED);
        var enabled = setting?.Value != null && bool.TryParse(setting.Value, out var parsed) && parsed;
        if (!enabled)
        {
            return;
        }

        var clientRepository = _serviceProvider.GetRequiredService<IClientRepository>();
        var client = await clientRepository.GetTypeAndDisplayNameAsync(message.ClientId, cancellationToken);
        if (client == null)
        {
            _logger.LogInformation(
                "Skipping messenger intent analysis for message {MessageId}: client {ClientId} not found",
                message.MessageId, message.ClientId);
            return;
        }

        var analysisRepository = _serviceProvider.GetRequiredService<IInboundAnalysisRepository>();
        if (await analysisRepository.GetBySourceAsync(InboundSourceKind.Messenger, message.MessageId, cancellationToken) != null)
        {
            _logger.LogInformation(
                "Skipping messenger intent analysis for message {MessageId}: it was already analyzed", message.MessageId);
            return;
        }

        var source = ToInboundSource(message, client.DisplayName);
        var clarificationRequest = new ClarificationRequest(
            ClientId: message.ClientId,
            ClientType: client.Type,
            Source: source,
            ReplyChannel: message.Channel,
            SenderAddress: message.Sender,
            EmailThread: null);
        var preAnalysis = await BeforeAnalysisSafelyAsync(clarificationRequest, cancellationToken);

        var analysis = preAnalysis.AnswerAnalysis
            ?? await _serviceProvider.GetRequiredService<IInboundIntentAnalysisService>()
                .AnalyzeAsync(message.ClientId, client.Type, source, cancellationToken);

        await analysisRepository.AddAsync(analysis, cancellationToken);
        await _serviceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();

        var clarificationContext = preAnalysis.NotifierContext;
        if (preAnalysis.AnswerAnalysis == null)
        {
            var postAnalysis = await AfterAnalysisSafelyAsync(clarificationRequest, analysis, cancellationToken);
            if (postAnalysis.QuestionSent)
            {
                return;
            }

            clarificationContext = ClarificationNotificationTexts.JoinContext(clarificationContext, postAnalysis.NotifierContext);
        }

        var actionOrchestrator = _serviceProvider.GetRequiredService<IInboundActionOrchestrator>();
        var actionOutcome = await actionOrchestrator.ExecuteAsync(message.ClientId, source, analysis, cancellationToken);

        string? periodLoadSummary = null;
        if (analysis.FromDate != null && analysis.ClientType != EntityTypeEnum.Customer)
        {
            var periodLoadService = _serviceProvider.GetRequiredService<IEmailPeriodLoadService>();
            periodLoadSummary = await periodLoadService.BuildSummaryAsync(
                message.ClientId, analysis.FromDate.Value,
                analysis.UntilDate ?? analysis.FromDate.Value, cancellationToken);
        }

        var analysisNotifier = _serviceProvider.GetRequiredService<IInboundAnalysisNotifier>();
        await analysisNotifier.NotifyAsync(source, analysis, actionOutcome, periodLoadSummary, clarificationContext, cancellationToken);
    }

    private async Task<ClarificationPreAnalysis> BeforeAnalysisSafelyAsync(
        ClarificationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _serviceProvider.GetRequiredService<IClarificationCoordinator>()
                .BeforeAnalysisAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Clarification answer check failed for messenger message {MessageId}; the message is analyzed normally",
                request.Source.SourceId);
            return ClarificationPreAnalysis.None;
        }
    }

    private async Task<ClarificationPostAnalysis> AfterAnalysisSafelyAsync(
        ClarificationRequest request, InboundAnalysis analysis, CancellationToken cancellationToken)
    {
        try
        {
            return await _serviceProvider.GetRequiredService<IClarificationCoordinator>()
                .AfterAnalysisAsync(request, analysis, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Clarification check failed for messenger message {MessageId}; the message stays on the regular path",
                request.Source.SourceId);
            return ClarificationPostAnalysis.Continue;
        }
    }

    private static InboundSource ToInboundSource(InboundClientMessengerMessage message, string clientDisplayName) => new(
        message.MessageId,
        InboundSourceKind.Messenger,
        $"{MessengerConstants.InboundChannelPrefix}{message.Channel}",
        ResolveSenderDisplay(message, clientDisplayName),
        null,
        message.Content,
        message.ReceivedAt);

    private static string ResolveSenderDisplay(InboundClientMessengerMessage message, string clientDisplayName)
    {
        if (!string.IsNullOrWhiteSpace(clientDisplayName))
        {
            return clientDisplayName;
        }

        return string.IsNullOrWhiteSpace(message.SenderDisplayName) ? message.Sender : message.SenderDisplayName;
    }
}
