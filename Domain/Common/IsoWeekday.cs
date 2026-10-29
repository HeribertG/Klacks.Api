// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Converts between the ISO weekday skills speak (1=Monday..7=Sunday) and the weekday container templates store
/// (System.DayOfWeek numbering, 0=Sunday..6=Saturday). Monday to Saturday carry the same number in both.
/// </summary>

namespace Klacks.Api.Domain.Common;

public static class IsoWeekday
{
    public const int Monday = 1;

    public const int Sunday = 7;

    public const string RangeError = "weekday must be between 1 (Monday) and 7 (Sunday).";

    private const int StorageSunday = (int)DayOfWeek.Sunday;

    public static bool IsValid(int isoWeekday) => isoWeekday >= Monday && isoWeekday <= Sunday;

    /// <summary>
    /// Storage weekday of a valid ISO weekday.
    /// </summary>
    /// <param name="isoWeekday">ISO weekday 1..7; callers validate it with IsValid first</param>
    public static int ToStorage(int isoWeekday) => isoWeekday == Sunday ? StorageSunday : isoWeekday;

    /// <summary>
    /// ISO weekday of a stored weekday.
    /// </summary>
    /// <param name="storageWeekday">Stored weekday 0=Sunday..6=Saturday</param>
    public static int FromStorage(int storageWeekday) => storageWeekday == StorageSunday ? Sunday : storageWeekday;
}
