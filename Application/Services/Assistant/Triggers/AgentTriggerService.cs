// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 4 skeleton implementation. Logs the event today; next iteration wires up
/// AssistantNotificationHub + per-user rate-limiting + severity-threshold settings.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class AgentTriggerService : IAgentTriggerService
{
    private readonly ILogger<AgentTriggerService> _logger;

    public AgentTriggerService(ILogger<AgentTriggerService> logger)
    {
        _logger = logger;
    }

    public Task OnEventAsync(IAgentTriggerEvent triggerEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AgentTrigger received {Kind} severity={Severity}: {Summary}",
            triggerEvent.Kind, triggerEvent.Severity, triggerEvent.Summary);
        // TODO Phase 4: dispatch to AssistantNotificationHub once it exists.
        return Task.CompletedTask;
    }
}
