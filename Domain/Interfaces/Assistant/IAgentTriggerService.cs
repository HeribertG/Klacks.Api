// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 4 (autonomy roadmap) — proactive trigger entry point.
/// Domain events (shift unstaffed N days ahead, lock conflict detected, period-hours drift, ...)
/// post here. Implementations decide severity, throttle by user preferences, then push a
/// notification via AssistantNotificationHub (Klacksy proactively writes to the user).
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentTriggerService
{
    Task OnEventAsync(IAgentTriggerEvent triggerEvent, CancellationToken cancellationToken = default);
}

public interface IAgentTriggerEvent
{
    string Kind { get; }
    string Severity { get; }
    string Summary { get; }
    IReadOnlyDictionary<string, object?> Payload { get; }
}
