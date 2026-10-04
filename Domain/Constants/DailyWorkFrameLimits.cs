// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Valid range of a daily work frame (first start to last end of one work day, pauses included) on a scheduling
/// rule or preset: empty inherits, 0 means no legal frame (24h minus the minimum rest), at most one day.
/// </summary>
/// <param name="hours">Daily work frame in hours; null means inherit</param>

namespace Klacks.Api.Domain.Constants;

public static class DailyWorkFrameLimits
{
    public const decimal MaxHours = 24m;

    public static bool IsInRange(decimal? hours) => hours is null or (>= 0m and <= MaxHours);
}
