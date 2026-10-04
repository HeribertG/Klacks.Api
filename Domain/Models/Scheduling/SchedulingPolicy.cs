// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Models.Scheduling;

/// <summary>
/// Effective scheduling-policy values for a single client at a given date. Resolved by combining
/// the global default settings with the client's contract / scheduling-rule overrides. Both the
/// wizard placement engine and the post-hoc validator MUST consume the same record so a value
/// accepted by one cannot be flagged by the other.
/// </summary>
/// <param name="MinRestHours">Minimum daily rest between two work days; the pauses inside a split shift are not
/// rest (see ClientTimeline.GetRestGaps)</param>
/// <param name="MaxDailyHours">Maximum hours per calendar day</param>
/// <param name="MaxConsecutiveDays">Maximum consecutive work days without a rest day</param>
/// <param name="MaxWeeklyHours">Maximum work hours per ISO week (Monday-anchored)</param>
/// <param name="MinRestDays">Minimum rest (non-work) days per ISO week; some jurisdictions require a
/// fractional weekly average (e.g. Spain ET Art. 37.1: 1.5 days/week)</param>
/// <param name="MaxDailySpan">Daily work frame set by the law of the place of work (first start to last end of a
/// work day, pauses included; CH ArG Art. 10 Abs. 3: 14h); null when the law sets none</param>
public sealed record SchedulingPolicy(
    TimeSpan MinRestHours,
    TimeSpan MaxDailyHours,
    int MaxConsecutiveDays,
    TimeSpan MaxWeeklyHours,
    decimal MinRestDays,
    TimeSpan? MaxDailySpan = null)
{
    /// <summary>
    /// Effective daily work frame used to group split shifts into one work day: <see cref="MaxDailySpan"/> when
    /// set, otherwise 24h minus <see cref="MinRestHours"/> (EU 2003/88 Art. 3).
    /// </summary>
    public TimeSpan DailyWorkFrame => MaxDailySpan is { } span && span > TimeSpan.Zero
        ? span
        : ClientTimeline.DefaultDailyWorkFrame(MinRestHours);
}
