// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Dispatches proactive trigger events to currently connected users via the assistant
/// notification hub. Each (user, kind) is rate-limited to AgentTriggerRateLimiter's
/// daily budget to avoid notification fatigue. Disconnected users are skipped silently;
/// their events are NOT replayed when they come back online (out of scope for S7).
/// </summary>
/// <param name="rateLimiter">Per-user-per-kind daily budget gate.</param>
/// <param name="notificationService">Pushes the proactive message via SignalR.</param>
/// <param name="logger">Structured log per dispatch.</param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class AgentTriggerService : IAgentTriggerService
{
    private readonly IAgentTriggerRateLimiter _rateLimiter;
    private readonly IAssistantNotificationService _notificationService;
    private readonly ILogger<AgentTriggerService> _logger;

    public AgentTriggerService(
        IAgentTriggerRateLimiter rateLimiter,
        IAssistantNotificationService notificationService,
        ILogger<AgentTriggerService> logger)
    {
        _rateLimiter = rateLimiter;
        _notificationService = notificationService;
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

        var message = FormatMessage(triggerEvent);
        var dispatched = 0;
        var throttled = 0;

        foreach (var userId in connectedUserIds)
        {
            if (!_rateLimiter.ShouldFire(userId, triggerEvent.Kind))
            {
                throttled++;
                continue;
            }

            try
            {
                await _notificationService.SendProactiveMessageAsync(userId, message);
                _rateLimiter.RecordFire(userId, triggerEvent.Kind);
                dispatched++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Trigger {Kind} dispatch failed for user {UserId}", triggerEvent.Kind, userId);
            }
        }

        _logger.LogInformation(
            "Trigger {Kind} severity={Severity} dispatched to {Dispatched} user(s), {Throttled} throttled. Summary: {Summary}",
            triggerEvent.Kind, triggerEvent.Severity, dispatched, throttled, triggerEvent.Summary);
    }

    private static string FormatMessage(IAgentTriggerEvent triggerEvent)
    {
        var severityTag = triggerEvent.Severity switch
        {
            "high" => "[HIGH] ",
            "medium" => "[MEDIUM] ",
            _ => ""
        };
        return $"{severityTag}{triggerEvent.Summary}";
    }
}
