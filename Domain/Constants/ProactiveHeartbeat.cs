// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Timing of the proactive heartbeat, shared by the background service that runs it and by the action
/// dispatcher that reasons about it. Defined once because two of the rules depend on the interval
/// itself: the cascade guard asks "was this condition detected in the first tick after a Klacksy
/// execution", and the stale-claim window has to be shorter than one interval or a crashed claim would
/// never be picked up again. A second, private copy of the interval would let those rules drift apart
/// from the schedule they describe without anything failing.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ProactiveHeartbeat
{
    /// <summary>Minutes between two detector scans.</summary>
    public const int ScanIntervalMinutes = 60;

    /// <summary>Minutes the first scan is delayed after start-up, so the application is warmed up.</summary>
    public const int FirstRunDelayMinutes = 2;
}
