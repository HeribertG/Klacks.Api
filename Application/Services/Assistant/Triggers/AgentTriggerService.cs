// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Dispatches proactive trigger events to their audience. The recipient set is derived from the
/// event itself (target user, planner/admin audience, or the currently connected users for
/// companion broadcasts), not from who happens to be online. Per recipient the pipeline is:
/// preference check (mute / snooze / minimum-severity), persisted dedup, daily rate-limit — then
/// the event is ALWAYS persisted as an inbox row (also for offline users). A live chat push via
/// SignalR happens only for connected recipients when the severity is high or the event is a
/// companion trigger; all other connected recipients receive a lightweight inbox-changed signal.
/// </summary>
/// <param name="rateLimiter">Per-user-per-kind daily budget gate.</param>
/// <param name="preferenceService">Per-user mute / snooze / severity threshold.</param>
/// <param name="notificationService">Pushes proactive messages and inbox changes via SignalR.</param>
/// <param name="dispatchRepository">Persists dispatch rows serving as dedup log and inbox.</param>
/// <param name="activityTracker">Suppresses live pushes while the user is actively chatting.</param>
/// <param name="planningAudienceResolver">Resolves the full planner / admin audience.</param>
/// <param name="logger">Structured log per dispatch.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class AgentTriggerService : IAgentTriggerService
{
    private static readonly TimeSpan ActiveConversationWindow = TimeSpan.FromMinutes(3);

    private readonly IAgentTriggerRateLimiter _rateLimiter;
    private readonly IAgentTriggerPreferenceService _preferenceService;
    private readonly IAssistantNotificationService _notificationService;
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IUserActivityTracker _activityTracker;
    private readonly IPlanningAudienceResolver _planningAudienceResolver;
    private readonly ILogger<AgentTriggerService> _logger;

    public AgentTriggerService(
        IAgentTriggerRateLimiter rateLimiter,
        IAgentTriggerPreferenceService preferenceService,
        IAssistantNotificationService notificationService,
        IProactiveTriggerDispatchRepository dispatchRepository,
        IUserActivityTracker activityTracker,
        IPlanningAudienceResolver planningAudienceResolver,
        ILogger<AgentTriggerService> logger)
    {
        _rateLimiter = rateLimiter;
        _preferenceService = preferenceService;
        _notificationService = notificationService;
        _dispatchRepository = dispatchRepository;
        _activityTracker = activityTracker;
        _planningAudienceResolver = planningAudienceResolver;
        _logger = logger;
    }

    public async Task OnEventAsync(IAgentTriggerEvent triggerEvent, CancellationToken cancellationToken = default)
    {
        var connectedUserIds = (await _notificationService.GetConnectedUserIdsAsync()).ToList();
        var recipients = await ResolveRecipientsAsync(triggerEvent, connectedUserIds, cancellationToken);
        if (recipients.Count == 0)
        {
            _logger.LogDebug("Trigger {Kind} skipped — no recipients", triggerEvent.Kind);
            return;
        }

        var connectedLookup = BuildConnectedLookup(connectedUserIds);
        var message = FormatMessage(triggerEvent);
        var contentParamsJson = BuildCappedParamsJson(triggerEvent.SummaryParams, ProactiveTriggerDispatchLimits.ContentParamsJsonMaxLength);
        var actionParamsJson = BuildCappedParamsJson(triggerEvent.ActionParams, ProactiveTriggerDispatchLimits.ActionParamsJsonMaxLength);
        var persisted = 0;
        var livePushed = 0;
        var inboxSignaled = 0;
        var throttled = 0;
        var muted = 0;
        var deduped = 0;

        foreach (var userId in recipients)
        {
            if (!await _preferenceService.IsAllowedAsync(userId, triggerEvent.Kind, triggerEvent.Severity))
            {
                muted++;
                continue;
            }

            if (await _dispatchRepository.WasDispatchedAsync(userId, triggerEvent.Kind, triggerEvent.DedupKey, cancellationToken))
            {
                deduped++;
                continue;
            }

            if (!_rateLimiter.ShouldFire(userId, triggerEvent.Kind))
            {
                throttled++;
                continue;
            }

            var messageId = Guid.NewGuid();
            try
            {
                // Persist BEFORE any live delivery: inbox rows must exist even for offline users,
                // and a failed live push must not lose the message.
                await _dispatchRepository.RecordAsync(new ProactiveTriggerDispatchRow
                {
                    Id = messageId,
                    UserId = userId,
                    TriggerKind = triggerEvent.Kind,
                    DedupKey = triggerEvent.DedupKey,
                    ContentKey = triggerEvent.Summary,
                    ContentParamsJson = contentParamsJson,
                    Severity = triggerEvent.Severity,
                    ActionRoute = triggerEvent.ActionRoute,
                    ActionParamsJson = actionParamsJson
                }, cancellationToken);
                _rateLimiter.RecordFire(userId, triggerEvent.Kind);
                persisted++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Trigger {Kind} persistence failed for user {UserId}", triggerEvent.Kind, userId);
                continue;
            }

            if (!connectedLookup.TryGetValue(userId, out var deliveryUserId))
            {
                continue;
            }

            var (wasLivePushed, wasInboxSignaled) = await DeliverAsync(triggerEvent, userId, deliveryUserId, message, messageId, cancellationToken);
            if (wasLivePushed)
            {
                livePushed++;
            }

            if (wasInboxSignaled)
            {
                inboxSignaled++;
            }
        }

        _logger.LogInformation(
            "Trigger {Kind} severity={Severity} persisted for {Persisted} user(s) ({LivePushed} live, {InboxSignaled} inbox-signaled), {Throttled} throttled, {Muted} muted, {Deduped} deduped. Summary: {Summary}",
            triggerEvent.Kind, triggerEvent.Severity, persisted, livePushed, inboxSignaled, throttled, muted, deduped, triggerEvent.Summary);
    }

    private async Task<(bool LivePushed, bool InboxSignaled)> DeliverAsync(
        IAgentTriggerEvent triggerEvent,
        string userId,
        string deliveryUserId,
        string message,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        if (ShouldLivePush(triggerEvent, deliveryUserId))
        {
            try
            {
                await _notificationService.SendProactiveMessageAsync(
                    deliveryUserId,
                    message,
                    contentParams: triggerEvent.SummaryParams,
                    messageId: messageId.ToString(),
                    kind: triggerEvent.Kind,
                    actionRoute: triggerEvent.ActionRoute,
                    actionParams: triggerEvent.ActionParams);
                return (true, false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Trigger {Kind} live push failed for user {UserId}; the message stays reachable via the inbox", triggerEvent.Kind, userId);
                return (false, false);
            }
        }

        try
        {
            var unreadCount = await _dispatchRepository.CountUnreadAsync(userId, cancellationToken);
            await _notificationService.SendProactiveInboxChangedAsync(deliveryUserId, unreadCount);
            return (false, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trigger {Kind} inbox-changed signal failed for user {UserId}", triggerEvent.Kind, userId);
            return (false, false);
        }
    }

    private async Task<IReadOnlyList<string>> ResolveRecipientsAsync(
        IAgentTriggerEvent triggerEvent,
        IReadOnlyList<string> connectedUserIds,
        CancellationToken cancellationToken)
    {
        if (triggerEvent.TargetUserId is Guid targetUserId)
        {
            return [targetUserId.ToString()];
        }

        if (triggerEvent.AdminOnly)
        {
            var adminIds = await _planningAudienceResolver.GetAdminUserIdsAsync(cancellationToken);
            return adminIds.ToList();
        }

        if (triggerEvent.PlannersOnly)
        {
            var plannerIds = await _planningAudienceResolver.GetPlanningUserIdsAsync(cancellationToken);
            return plannerIds.ToList();
        }

        // Companion broadcasts (curiosity / onboarding style events without an audience gate) go to
        // currently connected users only — deliberately no mass persistence for every known user.
        return connectedUserIds;
    }

    private static Dictionary<string, string> BuildConnectedLookup(IReadOnlyList<string> connectedUserIds)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var connectedUserId in connectedUserIds)
        {
            lookup.TryAdd(connectedUserId, connectedUserId);
        }

        return lookup;
    }

    private bool ShouldLivePush(IAgentTriggerEvent triggerEvent, string userId)
    {
        if (_activityTracker.IsRecentlyActive(userId, ActiveConversationWindow))
        {
            return false;
        }

        if (string.Equals(triggerEvent.Severity, AgentTriggerSeverity.High, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IsCompanionEvent(triggerEvent);
    }

    private static bool IsCompanionEvent(IAgentTriggerEvent triggerEvent)
    {
        return !triggerEvent.PlannersOnly && !triggerEvent.AdminOnly;
    }

    private static string? BuildCappedParamsJson(IReadOnlyDictionary<string, string>? paramValues, int maxJsonLength)
    {
        if (paramValues == null)
        {
            return null;
        }

        var cappedParams = paramValues.ToDictionary(
            pair => pair.Key,
            pair => TruncateParamValue(pair.Value));
        var json = JsonSerializer.Serialize(cappedParams);
        return json.Length <= maxJsonLength ? json : null;
    }

    private static string TruncateParamValue(string value)
    {
        if (value.Length <= ProactiveTriggerDispatchLimits.ContentParamValueMaxLength)
        {
            return value;
        }

        var keepLength = ProactiveTriggerDispatchLimits.ContentParamValueMaxLength - ProactiveTriggerDispatchLimits.TruncationSuffix.Length;
        return value[..keepLength] + ProactiveTriggerDispatchLimits.TruncationSuffix;
    }

    private static string FormatMessage(IAgentTriggerEvent triggerEvent)
    {
        if (triggerEvent.Summary.StartsWith(ProactiveMessageMarkers.I18nPrefix, StringComparison.Ordinal))
        {
            return triggerEvent.Summary;
        }

        var severityTag = triggerEvent.Severity switch
        {
            AgentTriggerSeverity.High => "[HIGH] ",
            AgentTriggerSeverity.Medium => "[MEDIUM] ",
            _ => ""
        };
        return $"{severityTag}{triggerEvent.Summary}";
    }
}
