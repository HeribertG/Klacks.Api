// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Dispatches proactive trigger events to currently connected users via the assistant
/// notification hub. Filtering happens in two stages: per-user preference (mute / snooze /
/// minimum-severity) then per-user rate-limit (daily budget) to avoid notification fatigue.
/// Disconnected users are skipped silently; their events are NOT replayed when they come
/// back online (out of scope for S7/S8).
/// </summary>
/// <param name="rateLimiter">Per-user-per-kind daily budget gate.</param>
/// <param name="preferenceService">Per-user mute / snooze / severity threshold.</param>
/// <param name="notificationService">Pushes the proactive message via SignalR.</param>
/// <param name="logger">Structured log per dispatch.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

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
        var connectedUserIds = _notificationService.GetConnectedUserIds().ToList();
        if (connectedUserIds.Count == 0)
        {
            _logger.LogDebug("Trigger {Kind} skipped — no connected users", triggerEvent.Kind);
            return;
        }

        if (triggerEvent.TargetUserId is Guid targetUserId)
        {
            var targetId = targetUserId.ToString();
            connectedUserIds = connectedUserIds
                .Where(u => string.Equals(u, targetId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (connectedUserIds.Count == 0)
            {
                _logger.LogDebug("Trigger {Kind} skipped — target user not connected", triggerEvent.Kind);
                return;
            }
        }

        if (triggerEvent.PlannersOnly)
        {
            var plannerIds = await _planningAudienceResolver.GetPlanningUserIdsAsync(cancellationToken);
            connectedUserIds = connectedUserIds
                .Where(u => plannerIds.Contains(u))
                .ToList();
            if (connectedUserIds.Count == 0)
            {
                _logger.LogDebug("Trigger {Kind} skipped — no connected planners", triggerEvent.Kind);
                return;
            }
        }

        var message = FormatMessage(triggerEvent);
        var dispatched = 0;
        var throttled = 0;
        var muted = 0;
        var deduped = 0;
        var busy = 0;

        foreach (var userId in connectedUserIds)
        {
            if (!await _preferenceService.IsAllowedAsync(userId, triggerEvent.Kind, triggerEvent.Severity))
            {
                muted++;
                continue;
            }

            // Do not interrupt an active conversation with a proactive alert.
            if (_activityTracker.IsRecentlyActive(userId, ActiveConversationWindow))
            {
                busy++;
                continue;
            }

            // Content dedup: never send the same alert (kind + content) to a user twice — survives restarts.
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

            try
            {
                await _notificationService.SendProactiveMessageAsync(userId, message, contentParams: triggerEvent.SummaryParams);
                await _dispatchRepository.RecordAsync(userId, triggerEvent.Kind, triggerEvent.DedupKey, cancellationToken);
                _rateLimiter.RecordFire(userId, triggerEvent.Kind);
                dispatched++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Trigger {Kind} dispatch failed for user {UserId}", triggerEvent.Kind, userId);
            }
        }

        _logger.LogInformation(
            "Trigger {Kind} severity={Severity} dispatched to {Dispatched} user(s), {Throttled} throttled, {Muted} muted, {Deduped} deduped, {Busy} busy. Summary: {Summary}",
            triggerEvent.Kind, triggerEvent.Severity, dispatched, throttled, muted, deduped, busy, triggerEvent.Summary);
    }

    private static string FormatMessage(IAgentTriggerEvent triggerEvent)
    {
        // i18n summaries (rendered + interpolated in the user's UI language by the frontend) must keep
        // their bare "i18n:" prefix, so no literal severity tag is prepended here.
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
