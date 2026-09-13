// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What one call to IAgentTriggerService.OnEventAsync did across the event's recipients, so the
/// background tick can sum per-detector counts into one structured log line per tick instead of the
/// pipeline staying invisible whenever the host's default log level is above Information.
/// </summary>
/// <param name="Persisted">Recipients whose dispatch row was written.</param>
/// <param name="Throttled">Recipients blocked by their per-user daily rate limit.</param>
/// <param name="Muted">Recipients blocked by mute / snooze / minimum-severity preferences.</param>
/// <param name="Deduped">Recipients who already had this event's dedup key on an earlier dispatch row.</param>
/// <param name="Failed">Recipients whose dispatch row failed to persist.</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ProactiveDispatchOutcome(
    int Persisted,
    int Throttled,
    int Muted,
    int Deduped,
    int Failed)
{
    public static readonly ProactiveDispatchOutcome Empty = new(0, 0, 0, 0, 0);

    public ProactiveDispatchOutcome Add(ProactiveDispatchOutcome other) => new(
        Persisted + other.Persisted,
        Throttled + other.Throttled,
        Muted + other.Muted,
        Deduped + other.Deduped,
        Failed + other.Failed);
}
