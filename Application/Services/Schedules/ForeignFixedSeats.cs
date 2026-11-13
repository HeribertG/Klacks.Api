// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Takes the seats out of the Wizard 1 demand that a person outside the run already holds with a fixed work: per held
/// work exactly one single seat of the same (shift, date), never below zero. The apply keeps such a work, so offering the
/// seat would book it twice. A slot whose every seat is held leaves the shift list.
/// </summary>
/// <param name="shifts">Single-seat slots of the run (one CoreShift per seat)</param>
/// <param name="heldSeats">Slot (shift, date) of each fixed work held outside the run, one entry per work</param>
public static class ForeignFixedSeats
{
    public static IReadOnlyList<CoreShift> Remove(
        IReadOnlyList<CoreShift> shifts, IReadOnlyList<(Guid ShiftId, DateOnly Date)> heldSeats)
    {
        if (heldSeats.Count == 0)
        {
            return shifts;
        }

        var toRemove = heldSeats
            .GroupBy(seat => seat)
            .ToDictionary(group => group.Key, group => group.Count());
        var kept = new List<CoreShift>(shifts.Count);
        foreach (var shift in shifts)
        {
            if (OfferedSlots.TryKey(shift, out var key) && toRemove.TryGetValue(key, out var pending) && pending > 0)
            {
                toRemove[key] = pending - 1;
                continue;
            }

            kept.Add(shift);
        }

        return kept.Count == shifts.Count ? shifts : kept;
    }
}
