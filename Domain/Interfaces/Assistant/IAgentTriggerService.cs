// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 4 (autonomy roadmap) — proactive trigger entry point.
/// Domain events (shift unstaffed N days ahead, lock conflict detected, period-hours drift, ...)
/// post here. Implementations decide severity, throttle by user preferences, then push a
/// notification via AssistantNotificationHub (Klacksy proactively writes to the user).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentTriggerService
{
    /// <summary>Returns the per-recipient dispatch outcome so a caller can aggregate tick-level telemetry.</summary>
    Task<ProactiveDispatchOutcome> OnEventAsync(IAgentTriggerEvent triggerEvent, CancellationToken cancellationToken = default);
}
