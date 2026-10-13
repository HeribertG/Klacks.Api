// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure rule that mirrors what the shift edit form leaves editable once a shift is no longer a draft order.
/// A shift with status SealedOrder, OriginalShift or SplitShift keeps its content: name, abbreviation, description,
/// dates, times, weekdays, headcount, address and client, type flags and its status can no longer be changed, and the
/// shift can no longer be deleted, with one exception: an administrator may delete a whole sealed order family while no
/// work, scenario clone or container reference exists on it (IsOrderFamilyDeletable names the shifts that qualify; the
/// service that deletes the family checks the usage and the caller checks the role). Still editable are the groups,
/// default expenses, required qualifications, the macro
/// and an end date that is still empty (set once). The cut hierarchy fields, the ERP and scenario reference fields and
/// the audit fields are not compared: the repository preserves them on a form save. Time-of-day fields are compared at
/// minute resolution and WorkTime within one minute, because the edit form rounds both to whole minutes on every save.
/// Writers that must change a locked shift (cut editor, order supersession, sealing, set-end-date skill, scenario
/// accept) write through the repository or the database context and never reach this policy.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class LockedShiftEditPolicy
{
    private const decimal WorkTimeToleranceHours = 1m / 60m;

    private static readonly (string Field, Func<Shift, Shift, bool> Differs)[] LockedFields =
    [
        (nameof(Shift.Status), (stored, incoming) => stored.Status != incoming.Status),
        (nameof(Shift.Name), (stored, incoming) => TextDiffers(stored.Name, incoming.Name)),
        (nameof(Shift.Abbreviation), (stored, incoming) => TextDiffers(stored.Abbreviation, incoming.Abbreviation)),
        (nameof(Shift.Description), (stored, incoming) => TextDiffers(stored.Description, incoming.Description)),
        (nameof(Shift.FromDate), (stored, incoming) => stored.FromDate != incoming.FromDate),
        (nameof(Shift.StartShift), (stored, incoming) => TimeDiffers(stored.StartShift, incoming.StartShift)),
        (nameof(Shift.EndShift), (stored, incoming) => TimeDiffers(stored.EndShift, incoming.EndShift)),
        (nameof(Shift.BeforeShift), (stored, incoming) => TimeDiffers(stored.BeforeShift, incoming.BeforeShift)),
        (nameof(Shift.AfterShift), (stored, incoming) => TimeDiffers(stored.AfterShift, incoming.AfterShift)),
        (nameof(Shift.BriefingTime), (stored, incoming) => TimeDiffers(stored.BriefingTime, incoming.BriefingTime)),
        (nameof(Shift.DebriefingTime), (stored, incoming) => TimeDiffers(stored.DebriefingTime, incoming.DebriefingTime)),
        (nameof(Shift.TravelTimeBefore), (stored, incoming) => TimeDiffers(stored.TravelTimeBefore, incoming.TravelTimeBefore)),
        (nameof(Shift.TravelTimeAfter), (stored, incoming) => TimeDiffers(stored.TravelTimeAfter, incoming.TravelTimeAfter)),
        (nameof(Shift.WorkTime), (stored, incoming) => Math.Abs(stored.WorkTime - incoming.WorkTime) > WorkTimeToleranceHours),
        (nameof(Shift.IsMonday), (stored, incoming) => stored.IsMonday != incoming.IsMonday),
        (nameof(Shift.IsTuesday), (stored, incoming) => stored.IsTuesday != incoming.IsTuesday),
        (nameof(Shift.IsWednesday), (stored, incoming) => stored.IsWednesday != incoming.IsWednesday),
        (nameof(Shift.IsThursday), (stored, incoming) => stored.IsThursday != incoming.IsThursday),
        (nameof(Shift.IsFriday), (stored, incoming) => stored.IsFriday != incoming.IsFriday),
        (nameof(Shift.IsSaturday), (stored, incoming) => stored.IsSaturday != incoming.IsSaturday),
        (nameof(Shift.IsSunday), (stored, incoming) => stored.IsSunday != incoming.IsSunday),
        (nameof(Shift.IsHoliday), (stored, incoming) => stored.IsHoliday != incoming.IsHoliday),
        (nameof(Shift.IsWeekdayAndHoliday), (stored, incoming) => stored.IsWeekdayAndHoliday != incoming.IsWeekdayAndHoliday),
        (nameof(Shift.IsSporadic), (stored, incoming) => stored.IsSporadic != incoming.IsSporadic),
        (nameof(Shift.SporadicScope), (stored, incoming) => stored.SporadicScope != incoming.SporadicScope),
        (nameof(Shift.IsTimeRange), (stored, incoming) => stored.IsTimeRange != incoming.IsTimeRange),
        (nameof(Shift.Quantity), (stored, incoming) => stored.Quantity != incoming.Quantity),
        (nameof(Shift.SumEmployees), (stored, incoming) => stored.SumEmployees != incoming.SumEmployees),
        (nameof(Shift.ShiftType), (stored, incoming) => stored.ShiftType != incoming.ShiftType),
        (nameof(Shift.ClientId), (stored, incoming) => stored.ClientId != incoming.ClientId),
        (nameof(Shift.CuttingAfterMidnight), (stored, incoming) => stored.CuttingAfterMidnight != incoming.CuttingAfterMidnight),
        (nameof(Shift.UntilDate), (stored, incoming) => stored.UntilDate != null && stored.UntilDate != incoming.UntilDate)
    ];

    public static bool Applies(Shift stored) =>
        stored.AnalyseToken == null && stored.Status != ShiftStatus.OriginalOrder;

    public static IReadOnlyList<string> FindLockedChanges(Shift stored, Shift incoming)
    {
        if (!Applies(stored))
        {
            return [];
        }

        return LockedFields
            .Where(entry => entry.Differs(stored, incoming))
            .Select(entry => entry.Field)
            .ToList();
    }

    public static IReadOnlyList<string> LockedFieldNames { get; } = LockedFields.Select(entry => entry.Field).ToList();

    public static bool IsDeletionLocked(Shift stored) => Applies(stored);

    public static bool IsOrderFamilyDeletable(Shift stored) =>
        stored.Status == ShiftStatus.SealedOrder && stored.AnalyseToken == null && stored.ScenarioSourceShiftId == null;

    private static bool TextDiffers(string? stored, string? incoming) =>
        !string.Equals(stored ?? string.Empty, incoming ?? string.Empty, StringComparison.Ordinal);

    private static bool TimeDiffers(TimeOnly stored, TimeOnly incoming) =>
        stored.Hour != incoming.Hour || stored.Minute != incoming.Minute;
}
