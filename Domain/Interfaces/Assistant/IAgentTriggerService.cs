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

    /// <summary>
    /// When set, the event is delivered only to this single user instead of being broadcast to
    /// all connected users. Null (the default) preserves the broadcast behaviour of domain triggers.
    /// </summary>
    Guid? TargetUserId => null;

    /// <summary>
    /// Stable content key used to deduplicate proactive notifications: the same key is delivered to a
    /// user at most once (persisted), so a recurring scan never re-sends the same alert. Defaults to
    /// the full Summary; events override it to ignore changing magnitudes (e.g. drift uses client+period).
    /// </summary>
    string DedupKey => Summary;
}
