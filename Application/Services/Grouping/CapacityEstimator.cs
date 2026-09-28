// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Arithmetic capacity check (F7) per planning unit and weekday, after all proposals: the peak of the
/// summed quantities of scope shifts running on that weekday that overlap in time (end exclusive; a shift
/// ending at or before its start runs past midnight) against the number of scope clients eligible for at
/// least one peak shift on that weekday. A necessary, not a sufficient condition: hours, rest days and
/// absences are ignored on purpose, and the overhang of the previous day's night shift is not counted.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class CapacityEstimator
{
    private static readonly DayOfWeek[] IsoWeek =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    ];

    public IReadOnlyList<GroupingFinding> Estimate(CapacityInput input)
    {
        var focus = input.FocusGroupId is Guid focusId && input.Tree.Contains(focusId)
            ? input.Tree.SelfAndDescendants(focusId)
            : null;
        var findings = new List<GroupingFinding>();

        var units = input.State.PlanningUnits()
            .Where(unit => focus is null || focus.Contains(unit))
            .OrderBy(unit => input.Tree.Get(unit).Name, StringComparer.Ordinal)
            .ThenBy(unit => unit);

        foreach (var unit in units)
        {
            var scopeShifts = input.State.ScopeShifts(unit, input.Tree);
            var scopeClients = input.State.ScopeClients(unit, input.Tree);

            foreach (var weekday in IsoWeek)
            {
                var running = scopeShifts
                    .Where(shiftId => input.RunDays.TryGetValue(shiftId, out var days) && days.Any(day => day.DayOfWeek == weekday))
                    .Select(shiftId => input.Shifts[shiftId])
                    .ToList();
                if (running.Count == 0)
                {
                    continue;
                }

                var (demand, peakShifts) = PeakDemand(running);
                var supply = scopeClients.Count(client =>
                    peakShifts.Any(shift => input.Eligibility.EligibleWeekdays(client, shift).Contains(weekday)));

                if (demand > supply)
                {
                    findings.Add(new GroupingFinding(
                        GroupingFindingCode.CapacityShortfall,
                        ReportOnly: true,
                        GroupId: unit == GroupingFeasibilityDefaults.NewGroupPlaceholderId ? null : unit,
                        Weekday: weekday,
                        Demand: demand,
                        Supply: supply));
                }
            }
        }

        return findings;
    }

    private static (int Demand, IReadOnlyList<Guid> PeakShifts) PeakDemand(IReadOnlyList<GroupingShiftRecord> shifts)
    {
        var events = shifts
            .SelectMany(shift =>
            {
                var start = ToMinutes(shift.Start);
                var end = ToMinutes(shift.End);
                if (end <= start)
                {
                    end += GroupingFeasibilityDefaults.MinutesPerDay;
                }

                var quantity = shift.Quantity > 0 ? shift.Quantity : 1;
                return new[] { (Time: start, Delta: quantity, shift.Id), (Time: end, Delta: -quantity, shift.Id) };
            })
            .OrderBy(e => e.Time)
            .ThenBy(e => e.Delta)
            .ToList();

        var active = new HashSet<Guid>();
        var current = 0;
        var best = 0;
        IReadOnlyList<Guid> bestSet = [];
        foreach (var (_, delta, id) in events)
        {
            current += delta;
            if (delta > 0)
            {
                active.Add(id);
            }
            else
            {
                active.Remove(id);
            }

            if (current > best)
            {
                best = current;
                bestSet = active.ToList();
            }
        }

        return (best, bestSet);
    }

    private static int ToMinutes(TimeOnly time) => time.Hour * GroupingFeasibilityDefaults.MinutesPerHour + time.Minute;
}
