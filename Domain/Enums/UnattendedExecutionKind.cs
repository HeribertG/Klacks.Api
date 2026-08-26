// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The background path a skill run originates from. Only ScheduledTask can carry a per-task opt-in for
/// irreversible skills; ProactiveHeartbeat has no such opt-in and ignores the flag entirely.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum UnattendedExecutionKind
{
    ScheduledTask = 0,
    ProactiveHeartbeat = 1
}
