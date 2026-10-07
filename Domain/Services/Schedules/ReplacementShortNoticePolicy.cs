// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Judges whether a replacement request was short notice: the absence was reported less than the configured
/// number of hours before the slot started. Evaluated when reading, so changing the threshold re-judges the
/// whole history instead of freezing an old verdict into the rows.
/// </summary>
/// <param name="reportedAtUtc">Instant the absence was reported</param>
/// <param name="shiftStartUtc">Instant the slot starts</param>
/// <param name="thresholdHours">REPLACEMENT_SHORT_NOTICE_HOURS</param>

namespace Klacks.Api.Domain.Services.Schedules;

public static class ReplacementShortNoticePolicy
{
    public static bool IsShortNotice(DateTime reportedAtUtc, DateTime shiftStartUtc, int thresholdHours)
        => shiftStartUtc - reportedAtUtc < TimeSpan.FromHours(thresholdHours);
}
