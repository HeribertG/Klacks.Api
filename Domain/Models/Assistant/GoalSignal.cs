// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A single aggregated observation collected by an IGoalSignalSource for one user, describing how
/// often something recurring was noticed and over which time span. Not persisted — this is the
/// reflection cycle's transient input, distinct from the persisted GoalCandidate it may lead to.
/// </summary>
/// <param name="UserId">User the signal was observed for.</param>
/// <param name="Kind">Stable identifier of what was observed, e.g. an AgentTriggerKinds value.</param>
/// <param name="Summary">Short human-readable description handed to the reflection LLM prompt.</param>
/// <param name="OccurrenceCount">How many times this signal occurred within the lookback window.</param>
/// <param name="FirstSeenUtc">Timestamp of the earliest occurrence within the lookback window.</param>
/// <param name="LastSeenUtc">Timestamp of the most recent occurrence within the lookback window.</param>

namespace Klacks.Api.Domain.Models.Assistant;

public record GoalSignal(
    string UserId,
    string Kind,
    string Summary,
    int OccurrenceCount,
    DateTime FirstSeenUtc,
    DateTime LastSeenUtc);
