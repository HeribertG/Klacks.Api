// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Concrete narrow delivery path for the escalation chain (see IEscalationNotifier). Reuses the same
/// primitives AgentTriggerService.OnEventAsync delivers through, but calls them directly instead of
/// going through that method, so mute/daily-budget/dedup never see this traffic (decision B5).
/// </summary>
/// <param name="dispatchRepository">Writes the inbox row every notification and handoff note leaves behind.</param>
/// <param name="notificationService">Live SignalR push and connection lookup for connected recipients.</param>
/// <param name="offlineMessengerNotifier">The loud channel; tried unconditionally per Owner decision A1.</param>
/// <param name="messengerTextComposer">Renders the wake-up sentence in the installation language.</param>
/// <param name="settingsReader">Reads DEFAULT_LANGUAGE for the two handoff sentences, mirroring ProactiveMessengerTextComposer.</param>
/// <param name="logger">Logs a delivery failure without ever aborting the caller's sweep.</param>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Application.Services.Assistant.Escalation;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Infrastructure.Services.Assistant.Escalation;

public class EscalationNotifier : IEscalationNotifier
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IAssistantNotificationService _notificationService;
    private readonly IOfflineMessengerNotifier _offlineMessengerNotifier;
    private readonly IProactiveMessengerTextComposer _messengerTextComposer;
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<EscalationNotifier> _logger;

    public EscalationNotifier(
        IProactiveTriggerDispatchRepository dispatchRepository,
        IAssistantNotificationService notificationService,
        IOfflineMessengerNotifier offlineMessengerNotifier,
        IProactiveMessengerTextComposer messengerTextComposer,
        ISettingsReader settingsReader,
        ILogger<EscalationNotifier> logger)
    {
        _dispatchRepository = dispatchRepository;
        _notificationService = notificationService;
        _offlineMessengerNotifier = offlineMessengerNotifier;
        _messengerTextComposer = messengerTextComposer;
        _settingsReader = settingsReader;
        _logger = logger;
    }

    public async Task<EscalationNotificationResult> NotifyStageAsync(
        EscalationChain chain, EscalationStage stage, DateTime dueAtUtc, CancellationToken cancellationToken = default)
    {
        var triggerEvent = new EscalationStageAlertTriggerEvent(
            stage.Id, stage.UserId, chain.AbsentClientName, chain.ShiftStartUtc, dueAtUtc);

        var messengerText = await ComposeSafelyAsync(triggerEvent, cancellationToken);
        var dispatchRowId = Guid.NewGuid();

        await _dispatchRepository.RecordAsync(new ProactiveTriggerDispatchRow
        {
            Id = dispatchRowId,
            UserId = stage.UserId,
            TriggerKind = triggerEvent.Kind,
            DedupKey = triggerEvent.DedupKey,
            ContentKey = triggerEvent.Summary,
            ContentParamsJson = JsonSerializer.Serialize(triggerEvent.SummaryParams),
            Severity = triggerEvent.Severity
        }, cancellationToken);

        await TryLivePushIfConnectedAsync(stage.UserId, messengerText, triggerEvent, dispatchRowId, cancellationToken);

        // A1: the messenger is attempted unconditionally, even for a connected recipient - a live
        // push reaches an open tab, not necessarily a person watching it at 03:00.
        var result = await TrySendMessengerAsync(stage.UserId, messengerText, triggerEvent.Kind, cancellationToken);

        return new EscalationNotificationResult(result.Outcome, dispatchRowId, result.Channel);
    }

    public async Task NotifyHandoffAsync(
        EscalationChain chain,
        EscalationStage acknowledgedStage,
        IReadOnlyList<EscalationStage> previouslyNotifiedStages,
        CancellationToken cancellationToken = default)
    {
        var language = await ResolveLanguageAsync(cancellationToken);
        var dateText = chain.ShiftStartUtc.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture);

        if (EscalationHandoffTexts.TryGetText(EscalationHandoffTexts.AcknowledgedConfirmation, language, out var confirmTemplate))
        {
            var confirmText = Substitute(confirmTemplate, new Dictionary<string, string>
            {
                ["date"] = dateText,
                ["employee"] = chain.AbsentClientName
            });

            await RecordInboxOnlyAsync(acknowledgedStage.UserId, confirmText, cancellationToken);
            await TrySendMessengerAsync(acknowledgedStage.UserId, confirmText, AgentTriggerKinds.EscalationStageAlert, cancellationToken);
        }

        if (!EscalationHandoffTexts.TryGetText(EscalationHandoffTexts.HandoffQuietNote, language, out var noteTemplate))
        {
            return;
        }

        var noteText = Substitute(noteTemplate, new Dictionary<string, string>
        {
            ["date"] = dateText,
            ["employee"] = chain.AbsentClientName,
            ["responder"] = acknowledgedStage.UserDisplayName
        });

        foreach (var previous in previouslyNotifiedStages)
        {
            if (previous.Id == acknowledgedStage.Id)
            {
                continue;
            }

            // Deliberately no messenger send here: "leise" (B7's reference case, §6) means inbox-only,
            // so A does not get woken a second time just to learn that B took over.
            await RecordInboxOnlyAsync(previous.UserId, noteText, cancellationToken);
        }
    }

    private async Task RecordInboxOnlyAsync(string userId, string message, CancellationToken cancellationToken)
    {
        try
        {
            await _dispatchRepository.RecordAsync(new ProactiveTriggerDispatchRow
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TriggerKind = AgentTriggerKinds.EscalationStageAlert,
                DedupKey = $"escalation-handoff:{Guid.NewGuid()}",
                ContentKey = message,
                Severity = AgentTriggerSeverity.Medium
            }, cancellationToken);

            var connectedUserIds = await _notificationService.GetConnectedUserIdsAsync();
            if (connectedUserIds.Contains(userId, StringComparer.OrdinalIgnoreCase))
            {
                var unreadCount = await _dispatchRepository.CountUnreadAsync(userId, cancellationToken);
                await _notificationService.SendProactiveInboxChangedAsync(userId, unreadCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Escalation handoff note failed for user {UserId}", userId);
        }
    }

    private async Task TryLivePushIfConnectedAsync(
        string userId,
        string message,
        EscalationStageAlertTriggerEvent triggerEvent,
        Guid dispatchRowId,
        CancellationToken cancellationToken)
    {
        try
        {
            var connectedUserIds = await _notificationService.GetConnectedUserIdsAsync();
            if (!connectedUserIds.Contains(userId, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            await _notificationService.SendProactiveMessageAsync(
                userId,
                message,
                contentParams: triggerEvent.SummaryParams,
                messageId: dispatchRowId.ToString(),
                kind: triggerEvent.Kind);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Escalation stage live push failed for user {UserId}; the inbox row and messenger attempt stand", userId);
        }
    }

    private async Task<OfflineMessengerDeliveryResult> TrySendMessengerAsync(
        string userId, string message, string triggerKind, CancellationToken cancellationToken)
    {
        try
        {
            return await _offlineMessengerNotifier.TrySendAsync(userId, message, triggerKind, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Escalation messenger send threw for user {UserId}", userId);
            return OfflineMessengerDeliveryResult.ChannelUnavailable;
        }
    }

    private async Task<string> ComposeSafelyAsync(EscalationStageAlertTriggerEvent triggerEvent, CancellationToken cancellationToken)
    {
        try
        {
            return await _messengerTextComposer.ComposeAsync(triggerEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Escalation stage text composition failed for stage {StageId}; falling back to the raw key", triggerEvent.StageId);
            return triggerEvent.Summary;
        }
    }

    private async Task<string> ResolveLanguageAsync(CancellationToken cancellationToken)
    {
        try
        {
            var setting = await _settingsReader.GetSetting(SettingKeys.DefaultLanguage);
            var configured = setting?.Value;
            if (!string.IsNullOrWhiteSpace(configured)
                && LanguageConfig.SupportedLanguages.Contains(configured, StringComparer.OrdinalIgnoreCase))
            {
                return configured;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read the installation language for an escalation handoff note; falling back to {Language}", LanguageConfig.DefaultLanguageFallback);
        }

        return LanguageConfig.DefaultLanguageFallback;
    }

    private static string Substitute(string template, IReadOnlyDictionary<string, string> parameters)
    {
        var text = template;
        foreach (var pair in parameters)
        {
            text = text.Replace(
                MessengerProactiveTexts.PlaceholderPrefix + pair.Key + MessengerProactiveTexts.PlaceholderSuffix,
                pair.Value,
                StringComparison.Ordinal);
        }

        return text;
    }
}
