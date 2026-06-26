// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Decision for a due scheduled task at a given tick.
/// </summary>
public enum ScheduledTaskRunDecision
{
    /// <summary>Not yet due.</summary>
    NotDue,

    /// <summary>Due within the catch-up window; run it now.</summary>
    Fire,

    /// <summary>Due but too stale (missed during downtime); advance without running.</summary>
    SkipStale
}
