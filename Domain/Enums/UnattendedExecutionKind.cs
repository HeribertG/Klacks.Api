// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The background path a skill run originates from. Only ScheduledTask can carry a per-task opt-in for
/// irreversible skills; ProactiveHeartbeat has no such opt-in and ignores the flag entirely.
/// InboundAutomation is the third path, covering the channel-neutral inbound pipeline (email and
/// messenger): it has no opt-in either, but its own intent mapping already requires the highest
/// autonomy level for every irreversible action it can trigger, so the policy judges it against that
/// level instead of refusing outright.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum UnattendedExecutionKind
{
    ScheduledTask = 0,
    ProactiveHeartbeat = 1,
    InboundAutomation = 2
}
