// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// The (shift, date) key of the Wizard 1 slots, read with the same invariant IsoDate format WizardShiftBuilder writes, and
/// the warm-start filter on it: a warm-start assignment survives only on a slot the context still offers. No supply check
/// sees a slot without capacity, so an assignment on a slot the run does not offer (all seats held by fixed works outside
/// the run, a weekday the shift does not run, a day after the shift's end) would be placed without anything weighing it.
/// </summary>
/// <param name="shift">Single-seat slot of the run</param>
/// <param name="assignments">Warm-start assignments mapped onto the period</param>
/// <param name="offeredShifts">Slots the context offers</param>
public static class OfferedSlots
{
    public static bool TryKey(CoreShift shift, out (Guid ShiftId, DateOnly Date) key)
    {
        if (Guid.TryParse(shift.Id, out var shiftId)
            && DateOnly.TryParseExact(shift.Date, DateTimeFormats.IsoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            key = (shiftId, date);
            return true;
        }

        key = default;
        return false;
    }

    public static IReadOnlyList<CoreWarmStartAssignment> KeepWarmStartsOnOfferedSlots(
        IReadOnlyList<CoreWarmStartAssignment> assignments, IReadOnlyList<CoreShift> offeredShifts)
    {
        if (assignments.Count == 0)
        {
            return assignments;
        }

        var offered = new HashSet<(Guid ShiftId, DateOnly Date)>();
        foreach (var shift in offeredShifts)
        {
            if (TryKey(shift, out var key))
            {
                offered.Add(key);
            }
        }

        return assignments.Where(a => offered.Contains((a.ShiftRefId, a.Date))).ToList();
    }
}
