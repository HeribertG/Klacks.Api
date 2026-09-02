// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The reminder branch of package F ("repeat until acknowledged"): re-delivers persisted dispatch rows
/// whose user never reacted, on the backoff schedule of ProactiveReminderSchedule. Runs over the
/// dispatch rows the trigger pipeline left behind rather than over anything in memory, which is what
/// makes the whole branch restartable after a crash and safe to run on several API instances at once.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IProactiveReminderService
{
    /// <summary>
    /// One pass over the dispatch rows whose reminder is due, capped at
    /// ProactiveReminderDefaults.SweepBatchSize. Never throws for a single row's failure - a row whose
    /// processing fails is logged and keeps its due date, so the next sweep retries it.
    /// </summary>
    Task<ProactiveReminderSweepResult> RunAsync(CancellationToken cancellationToken = default);
}
