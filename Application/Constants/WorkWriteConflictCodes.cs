// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Machine codes and detail field names of a refused work write. The code travels as "errorCode" in the
/// 409 body so the client can show a translated sentence instead of the English diagnostic message; the
/// detail names are the extra fields next to it. Mirrored by work-conflict.constants.ts in Klacks.Ui.
/// </summary>
public static class WorkWriteConflictCodes
{
    /// <summary>The write would introduce a non-overridable schedule conflict, e.g. a missing mandatory qualification.</summary>
    public const string BlockedByConflicts = "WORK_BLOCKED_BY_CONFLICTS";

    /// <summary>A sporadic shift already has all its employees booked on that day.</summary>
    public const string SporadicShiftDayFull = "SPORADIC_SHIFT_DAY_FULL";

    /// <summary>A sporadic shift already uses all booked days its range allows, and the requested day is a new one.</summary>
    public const string SporadicShiftRangeExhausted = "SPORADIC_SHIFT_RANGE_EXHAUSTED";

    public const string ShiftIdField = "shiftId";

    public const string ShiftNameField = "shiftName";

    public const string DateField = "date";

    public const string EngagedField = "engaged";

    public const string BookedField = "booked";

    public const string CapacityField = "capacity";

    public const string RangeFromField = "rangeFrom";

    public const string RangeUntilField = "rangeUntil";

    public const string ConflictsField = "conflicts";
}
