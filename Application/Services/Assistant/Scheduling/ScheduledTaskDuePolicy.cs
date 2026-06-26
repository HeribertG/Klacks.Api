// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure decision brain of the scheduled-task tick: given a task's planned next run, the current time
/// and a catch-up window, decides whether to fire it now, skip a stale occurrence (the server was
/// offline at the planned time) or treat it as not yet due. Pure + deterministic so it is fully
/// unit-tested; the background service supplies the clock and persistence.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Assistant.Scheduling;

public sealed class ScheduledTaskDuePolicy
{
    public ScheduledTaskRunDecision Decide(DateTime? nextRunUtc, DateTime nowUtc, TimeSpan catchUpWindow)
    {
        if (nextRunUtc is not { } next)
        {
            return ScheduledTaskRunDecision.NotDue;
        }

        if (next > nowUtc)
        {
            return ScheduledTaskRunDecision.NotDue;
        }

        return nowUtc - next <= catchUpWindow
            ? ScheduledTaskRunDecision.Fire
            : ScheduledTaskRunDecision.SkipStale;
    }
}
