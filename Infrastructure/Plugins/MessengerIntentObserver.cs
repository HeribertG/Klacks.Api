// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges an inbound messenger message resolved to a known CLIENT (employee, extern employee,
/// customer) into the same channel-neutral intent-analysis/action-orchestration/notification kernel
/// the email pipeline uses (see EmailPollingBackgroundService.ProcessEmailAsync). Distinct from
/// MessagingPluginInboundMessageObserver, which reacts to messages resolved to an APP USER (escalation
/// replies) — the two paths never overlap because a message resolves to exactly one or the other.
/// </summary>
/// <param name="clientRepository">Resolves the client behind an inbound message and its EntityTypeEnum</param>
/// <param name="settingsRepository">Reads the MESSENGER_ANALYSIS_ENABLED feature gate</param>
/// <param name="intentAnalysisService">Runs the channel-neutral intent classification</param>
/// <param name="actionOrchestrator">Executes the action the analysis calls for, if any</param>
/// <param name="analysisRepository">Persists the resulting InboundAnalysis</param>
/// <param name="analysisNotifier">Notifies staff about the analysis and its outcome</param>
/// <param name="unitOfWork">Commits the staged InboundAnalysis write</param>
/// <param name="logger">Structured log of skip/processing decisions</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public sealed class MessengerIntentObserver : IInboundClientMessengerObserver
{
    private readonly IClientRepository _clientRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IInboundIntentAnalysisService _intentAnalysisService;
    private readonly IInboundActionOrchestrator _actionOrchestrator;
    private readonly IInboundAnalysisRepository _analysisRepository;
    private readonly IInboundAnalysisNotifier _analysisNotifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MessengerIntentObserver> _logger;

    public MessengerIntentObserver(
        IClientRepository clientRepository,
        ISettingsRepository settingsRepository,
        IInboundIntentAnalysisService intentAnalysisService,
        IInboundActionOrchestrator actionOrchestrator,
        IInboundAnalysisRepository analysisRepository,
        IInboundAnalysisNotifier analysisNotifier,
        IUnitOfWork unitOfWork,
        ILogger<MessengerIntentObserver> logger)
    {
        _clientRepository = clientRepository;
        _settingsRepository = settingsRepository;
        _intentAnalysisService = intentAnalysisService;
        _actionOrchestrator = actionOrchestrator;
        _analysisRepository = analysisRepository;
        _analysisNotifier = analysisNotifier;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task OnInboundMessageAsync(InboundClientMessengerMessage message, CancellationToken cancellationToken = default)
    {
        var setting = await _settingsRepository.GetSetting(Settings.MESSENGER_ANALYSIS_ENABLED);
        var enabled = setting?.Value != null && bool.TryParse(setting.Value, out var parsed) && parsed;
        if (!enabled)
        {
            return;
        }

        var client = await _clientRepository.GetNoTracking(message.ClientId);
        if (client == null)
        {
            _logger.LogInformation(
                "Skipping messenger intent analysis for message {MessageId}: client {ClientId} not found",
                message.MessageId, message.ClientId);
            return;
        }

        var channelLabel = $"Messenger:{message.Channel}";
        var source = new InboundSource(
            message.MessageId,
            InboundSourceKind.Messenger,
            channelLabel,
            string.IsNullOrWhiteSpace(message.SenderDisplayName) ? message.Sender : message.SenderDisplayName,
            null,
            message.Content,
            message.ReceivedAt);

        var analysis = await _intentAnalysisService.AnalyzeAsync(message.ClientId, client.Type, source, cancellationToken);

        await _analysisRepository.AddAsync(analysis, cancellationToken);
        await _unitOfWork.CompleteAsync();

        var actionOutcome = await _actionOrchestrator.ExecuteAsync(message.ClientId, source, analysis, cancellationToken);
        await _analysisNotifier.NotifyAsync(source, analysis, actionOutcome, periodLoadSummary: null, cancellationToken);
    }
}
