// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges an inbound messenger message resolved to a known CLIENT (employee, extern employee,
/// customer) into the same channel-neutral intent-analysis/action-orchestration/notification kernel
/// the email pipeline uses (see EmailPollingBackgroundService.ProcessEmailAsync). Distinct from
/// MessagingPluginInboundMessageObserver, which reacts to messages resolved to an APP USER (escalation
/// replies) — the two paths never overlap because a message resolves to exactly one or the other.
/// Depends only on IServiceScopeFactory and the logger on purpose: MessagingService receives this
/// observer through its constructor, and MessagingService itself sits deep inside the LLM/trigger
/// graph (ILLMService -> ... -> IOfflineMessengerNotifier -> MessagingService). Injecting the kernel
/// services directly closed that graph into a cycle and stopped the host from booting whenever the
/// messaging plugin was active. The kernel services are resolved per message from a fresh scope,
/// mirroring EmailPollingBackgroundService, which also keeps this observer's UnitOfWork commit apart
/// from the DbContext of the plugin request that persisted the message.
/// </summary>
/// <param name="scopeFactory">Creates the per-message scope the kernel services are resolved from</param>
/// <param name="logger">Structured log of skip/processing decisions</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public sealed class MessengerIntentObserver : IInboundClientMessengerObserver
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessengerIntentObserver> _logger;

    public MessengerIntentObserver(IServiceScopeFactory scopeFactory, ILogger<MessengerIntentObserver> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task OnInboundMessageAsync(InboundClientMessengerMessage message, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var settingsRepository = services.GetRequiredService<ISettingsRepository>();
        var setting = await settingsRepository.GetSetting(Settings.MESSENGER_ANALYSIS_ENABLED);
        var enabled = setting?.Value != null && bool.TryParse(setting.Value, out var parsed) && parsed;
        if (!enabled)
        {
            return;
        }

        var clientRepository = services.GetRequiredService<IClientRepository>();
        var clientType = await clientRepository.GetTypeAsync(message.ClientId, cancellationToken);
        if (clientType == null)
        {
            _logger.LogInformation(
                "Skipping messenger intent analysis for message {MessageId}: client {ClientId} not found",
                message.MessageId, message.ClientId);
            return;
        }

        var channelLabel = $"{MessengerConstants.InboundChannelPrefix}{message.Channel}";
        var source = new InboundSource(
            message.MessageId,
            InboundSourceKind.Messenger,
            channelLabel,
            string.IsNullOrWhiteSpace(message.SenderDisplayName) ? message.Sender : message.SenderDisplayName,
            null,
            message.Content,
            message.ReceivedAt);

        var intentAnalysisService = services.GetRequiredService<IInboundIntentAnalysisService>();
        var analysis = await intentAnalysisService.AnalyzeAsync(message.ClientId, clientType.Value, source, cancellationToken);

        var analysisRepository = services.GetRequiredService<IInboundAnalysisRepository>();
        await analysisRepository.AddAsync(analysis, cancellationToken);
        await services.GetRequiredService<IUnitOfWork>().CompleteAsync();

        var actionOrchestrator = services.GetRequiredService<IInboundActionOrchestrator>();
        var actionOutcome = await actionOrchestrator.ExecuteAsync(message.ClientId, source, analysis, cancellationToken);

        var analysisNotifier = services.GetRequiredService<IInboundAnalysisNotifier>();
        await analysisNotifier.NotifyAsync(source, analysis, actionOutcome, periodLoadSummary: null, cancellationToken);
    }
}
