// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Concrete narrow delivery path for the escalation chain (see IEscalationNotifier). Reuses the same
/// primitives AgentTriggerService.OnEventAsync delivers through, but calls them directly instead of
/// going through that method, so mute/daily-budget/dedup never see this traffic (decision B5). The two
/// chain purposes differ in channel and wording: an absence stage is written to the inbox, live-pushed
/// and ALWAYS also sent over the messenger (Owner decision A1), while a ProactiveApproval stage reaches
/// the inbox and the live push only (Owner decision 2026-09-20 - nobody is woken at night for a
/// container template) and speaks of the finding and the remediation instead of a shift.
/// </summary>
/// <param name="dispatchRepository">Writes the inbox row every notification and handoff note leaves behind.</param>
/// <param name="notificationService">Live SignalR push and connection lookup for connected recipients.</param>
/// <param name="offlineMessengerNotifier">The loud channel; tried unconditionally for absence stages, never for approvals.</param>
/// <param name="messengerTextComposer">Renders the absence wake-up sentence in the installation language.</param>
/// <param name="settingsReader">Reads DEFAULT_LANGUAGE for the handoff sentences, mirroring ProactiveMessengerTextComposer.</param>
/// <param name="companyClock">Resolves the company's configured time zone for rendering shift/due times.</param>
/// <param name="conditionRepository">Names the finding an approval chain is about.</param>
/// <param name="remediationRegistry">Names the remediation an approval chain would release.</param>
/// <param name="logger">Logs a delivery failure without ever aborting the caller's sweep.</param>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Application.Services.Assistant.Escalation;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Infrastructure.Services.Assistant.Escalation;

public class EscalationNotifier : IEscalationNotifier
{
    private const string UnknownFinding = "unknown finding";
    private const string UnknownAction = "unknown action";
    private const string HandoffDedupPrefix = "escalation-handoff:";

    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IAssistantNotificationService _notificationService;
    private readonly IOfflineMessengerNotifier _offlineMessengerNotifier;
    private readonly IProactiveMessengerTextComposer _messengerTextComposer;
    private readonly ISettingsReader _settingsReader;
    private readonly ICompanyClock _companyClock;
    private readonly IAgentConditionRepository _conditionRepository;
    private readonly IConditionRemediationRegistry _remediationRegistry;
    private readonly ILogger<EscalationNotifier> _logger;

    public EscalationNotifier(
        IProactiveTriggerDispatchRepository dispatchRepository,
        IAssistantNotificationService notificationService,
        IOfflineMessengerNotifier offlineMessengerNotifier,
        IProactiveMessengerTextComposer messengerTextComposer,
        ISettingsReader settingsReader,
        ICompanyClock companyClock,
        IAgentConditionRepository conditionRepository,
        IConditionRemediationRegistry remediationRegistry,
        ILogger<EscalationNotifier> logger)
    {
        _dispatchRepository = dispatchRepository;
        _notificationService = notificationService;
        _offlineMessengerNotifier = offlineMessengerNotifier;
        _messengerTextComposer = messengerTextComposer;
        _settingsReader = settingsReader;
        _companyClock = companyClock;
        _conditionRepository = conditionRepository;
        _remediationRegistry = remediationRegistry;
        _logger = logger;
    }

    public async Task<EscalationNotificationResult> NotifyStageAsync(
        EscalationChain chain, EscalationStage stage, DateTime dueAtUtc, CancellationToken cancellationToken = default)
    {
        var companyTimeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);

        if (chain.Purpose == EscalationChainPurpose.ProactiveApproval)
        {
            return await NotifyApprovalStageAsync(chain, stage, dueAtUtc, companyTimeZone, cancellationToken);
        }

        var triggerEvent = new EscalationStageAlertTriggerEvent(
            stage.Id, stage.UserId, chain.AbsentClientName, ResolveAnchorUtc(chain), dueAtUtc, companyTimeZone);

        var messengerText = await ComposeSafelyAsync(triggerEvent, cancellationToken);
        var dispatchRowId = await RecordInboxAsync(stage.UserId, triggerEvent, cancellationToken);

        await TryLivePushIfConnectedAsync(stage.UserId, messengerText, triggerEvent, dispatchRowId, cancellationToken);

        // A1: the messenger is attempted unconditionally, even for a connected recipient - a live
        // push reaches an open tab, not necessarily a person watching it at 03:00.
        var result = await TrySendMessengerAsync(stage.UserId, messengerText, triggerEvent.Kind, cancellationToken);

        return new EscalationNotificationResult(result.Outcome, dispatchRowId, result.Channel);
    }

    /// <summary>
    /// Inbox row plus live push, no messenger. The inbox row IS the delivery, so the result reports Sent
    /// over the inbox channel - anything else would make the chain service skip the stage as undeliverable
    /// and walk straight past every candidate.
    /// </summary>
    private async Task<EscalationNotificationResult> NotifyApprovalStageAsync(
        EscalationChain chain,
        EscalationStage stage,
        DateTime dueAtUtc,
        TimeZoneInfo companyTimeZone,
        CancellationToken cancellationToken)
    {
        var (finding, action) = await DescribeApprovalSubjectAsync(chain, cancellationToken);
        var triggerEvent = new EscalationApprovalRequestTriggerEvent(
            stage.Id, stage.UserId, chain.ConditionId ?? Guid.Empty, finding, action, dueAtUtc, companyTimeZone);

        var dispatchRowId = await RecordInboxAsync(stage.UserId, triggerEvent, cancellationToken);
        await TryLivePushIfConnectedAsync(stage.UserId, triggerEvent.Summary, triggerEvent, dispatchRowId, cancellationToken);

        return new EscalationNotificationResult(
            OfflineMessengerDeliveryOutcome.Sent, dispatchRowId, EscalationDeliveryChannels.Inbox);
    }

    public async Task NotifyHandoffAsync(
        EscalationChain chain,
        EscalationStage acknowledgedStage,
        IReadOnlyList<EscalationStage> previouslyNotifiedStages,
        CancellationToken cancellationToken = default)
    {
        var language = await ResolveLanguageAsync(cancellationToken);
        var isApproval = chain.Purpose == EscalationChainPurpose.ProactiveApproval;
        var parameters = isApproval
            ? await ApprovalHandoffParametersAsync(chain, cancellationToken)
            : await AbsenceHandoffParametersAsync(chain, cancellationToken);

        var confirmationKey = isApproval
            ? EscalationHandoffTexts.ApprovalAcknowledgedConfirmation
            : EscalationHandoffTexts.AcknowledgedConfirmation;
        var quietNoteKey = isApproval
            ? EscalationHandoffTexts.ApprovalHandoffQuietNote
            : EscalationHandoffTexts.HandoffQuietNote;

        if (EscalationHandoffTexts.TryGetText(confirmationKey, language, out var confirmTemplate))
        {
            var confirmText = Substitute(confirmTemplate, parameters);
            await RecordInboxOnlyAsync(acknowledgedStage.UserId, confirmText, cancellationToken);

            if (!isApproval)
            {
                await TrySendMessengerAsync(
                    acknowledgedStage.UserId, confirmText, AgentTriggerKinds.EscalationStageAlert, cancellationToken);
            }
        }

        if (!EscalationHandoffTexts.TryGetText(quietNoteKey, language, out var noteTemplate))
        {
            return;
        }

        parameters["responder"] = acknowledgedStage.UserDisplayName;
        var noteText = Substitute(noteTemplate, parameters);

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

    private async Task<Dictionary<string, string>> AbsenceHandoffParametersAsync(
        EscalationChain chain, CancellationToken cancellationToken)
    {
        var companyTimeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
        var dateText = TimeZoneInfo.ConvertTimeFromUtc(ResolveAnchorUtc(chain), companyTimeZone)
            .ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture);

        return new Dictionary<string, string>
        {
            ["date"] = dateText,
            ["employee"] = chain.AbsentClientName
        };
    }

    private async Task<Dictionary<string, string>> ApprovalHandoffParametersAsync(
        EscalationChain chain, CancellationToken cancellationToken)
    {
        var (finding, action) = await DescribeApprovalSubjectAsync(chain, cancellationToken);

        return new Dictionary<string, string>
        {
            [EscalationApprovalRequestTriggerEvent.FindingParameter] = finding,
            [EscalationApprovalRequestTriggerEvent.ActionParameter] = action
        };
    }

    /// <summary>
    /// The finding's kind and the remediation skill an approval chain is about, read off the ledger row
    /// and the code-only registry. A row that has vanished or a kind without remediation yields neutral
    /// placeholders rather than an exception - the notification must still go out.
    /// </summary>
    private async Task<(string Finding, string Action)> DescribeApprovalSubjectAsync(
        EscalationChain chain, CancellationToken cancellationToken)
    {
        if (chain.ConditionId is not Guid conditionId)
        {
            return (UnknownFinding, UnknownAction);
        }

        try
        {
            var condition = await _conditionRepository.GetByIdAsync(conditionId, cancellationToken);
            if (condition is null)
            {
                return (UnknownFinding, UnknownAction);
            }

            var action = _remediationRegistry.TryGetEntry(condition.TriggerKind, out var entry) && entry is not null
                ? entry.RemediationSkillName
                : UnknownAction;

            return (condition.TriggerKind, action);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not describe condition {ConditionId} for approval chain {ChainId}", conditionId, chain.Id);
            return (UnknownFinding, UnknownAction);
        }
    }

    private async Task<Guid> RecordInboxAsync(string userId, IAgentTriggerEvent triggerEvent, CancellationToken cancellationToken)
    {
        var dispatchRowId = Guid.NewGuid();

        await _dispatchRepository.RecordAsync(new ProactiveTriggerDispatchRow
        {
            Id = dispatchRowId,
            UserId = userId,
            TriggerKind = triggerEvent.Kind,
            DedupKey = triggerEvent.DedupKey,
            ContentKey = triggerEvent.Summary,
            ContentParamsJson = JsonSerializer.Serialize(triggerEvent.SummaryParams),
            Severity = triggerEvent.Severity
        }, cancellationToken);

        return dispatchRowId;
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
                DedupKey = HandoffDedupPrefix + Guid.NewGuid(),
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
        IAgentTriggerEvent triggerEvent,
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
            _logger.LogWarning(ex, "Escalation stage live push failed for user {UserId}; the inbox row stands", userId);
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

    /// <summary>The moment the absence texts refer to: the shift start, or the deadline for a chain without one.</summary>
    private static DateTime ResolveAnchorUtc(EscalationChain chain) => chain.ShiftStartUtc ?? chain.DeadlineUtc;

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
