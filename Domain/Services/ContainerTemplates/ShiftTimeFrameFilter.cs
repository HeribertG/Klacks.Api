// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The time-frame rule of the available-task list of a container template slot: a time-range task (IsTimeRange) is a
/// candidate when its window overlaps the slot's frame, a fixed task when it lies completely inside the frame and does
/// not cross midnight itself. A frame whose end lies before its start crosses midnight.
/// </summary>
/// <param name="fromTime">Start of the slot's time frame</param>
/// <param name="untilTime">End of the slot's time frame</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.ContainerTemplates;

public static class ShiftTimeFrameFilter
{
    public static IQueryable<Shift> Apply(IQueryable<Shift> query, TimeOnly fromTime, TimeOnly untilTime)
    {
        if (fromTime > untilTime)
        {
            return query.Where(s =>
                (s.IsTimeRange && (s.StartShift < untilTime || s.EndShift > fromTime)) ||
                (!s.IsTimeRange && s.StartShift < s.EndShift && (s.StartShift >= fromTime || s.EndShift <= untilTime)));
        }

        return query.Where(s =>
            (s.IsTimeRange && s.StartShift < untilTime && s.EndShift > fromTime) ||
            (!s.IsTimeRange && s.StartShift >= fromTime && s.EndShift <= untilTime && s.StartShift < s.EndShift));
    }
}
