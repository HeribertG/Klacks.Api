// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Range of a scenario built on a source scenario (scenario from scenario): the requested range widened to the source
/// scenario's own range. A child narrower than its source would carry the source's changes only around the requested
/// days, so accepting it would drop the rest of the source plan and the child view would be empty beyond them.
/// </summary>

namespace Klacks.Api.Application.Services.Schedules;

public static class ScenarioSourceRange
{
    /// <param name="fromDate">First day the flow itself writes</param>
    /// <param name="untilDate">Last day the flow itself writes</param>
    /// <param name="sourceFromDate">First day of the source scenario; null without a source</param>
    /// <param name="sourceUntilDate">Last day of the source scenario; null without a source</param>
    public static (DateOnly FromDate, DateOnly UntilDate) Widen(
        DateOnly fromDate, DateOnly untilDate, DateOnly? sourceFromDate, DateOnly? sourceUntilDate)
        => (sourceFromDate is { } sourceFrom && sourceFrom < fromDate ? sourceFrom : fromDate,
            sourceUntilDate is { } sourceUntil && sourceUntil > untilDate ? sourceUntil : untilDate);
}
