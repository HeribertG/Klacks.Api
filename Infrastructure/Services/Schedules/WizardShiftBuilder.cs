// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Models;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="IWizardShiftBuilder"/>.
/// Shifts are filtered by date range and weekday flag. Each (shift, date) tuple becomes
/// ShiftStaffingDemand.PerDay (Quantity x SumEmployees) single-seat CoreShifts, because every seeding strategy of
/// the optimizer places exactly one employee per CoreShift. Sporadic shifts are on-call shifts whose Quantity counts
/// days per period, not shifts per day; they carry no daily demand and are left to manual booking.
/// A shift day the shift schedule marks as a container child (get_shift_schedule.is_in_template_container, the marker
/// the shift table shows and ShiftStaffingDemand.RequiredOn reads) gets no seat: the container is planned as a whole
/// and keeps its own seats (owner rule 2026-11-15).
/// The order identity a slot belongs to is derived as RootId ?? OriginalId ?? Id, so a standalone shift
/// counts as its own object.
/// </summary>
/// <param name="context">EF Core database context</param>
/// <param name="shiftScheduleReader">Reads the shift schedule rows whose container marker decides which shift days are container children</param>
public sealed class WizardShiftBuilder : IWizardShiftBuilder
{
    private const int SingleSeat = 1;

    private readonly DataBaseContext _context;
    private readonly IShiftScheduleReader _shiftScheduleReader;

    public WizardShiftBuilder(DataBaseContext context, IShiftScheduleReader shiftScheduleReader)
    {
        _context = context;
        _shiftScheduleReader = shiftScheduleReader;
    }

    public async Task<IReadOnlyList<CoreShift>> BuildAsync(
        IReadOnlyList<Guid>? shiftIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct)
    {
        var query = _context.Shift
            .AsNoTracking()
            .Where(s => s.AnalyseToken == analyseToken
                && !s.IsSporadic
                && s.FromDate <= until
                && (s.UntilDate == null || s.UntilDate >= from));

        if (shiftIds is { Count: > 0 })
        {
            var ids = shiftIds.ToList();
            query = query.Where(s => ids.Contains(s.Id));
        }

        var shifts = await query.ToListAsync(ct);
        var containerChildDays = await LoadContainerChildDaysAsync(from, until, analyseToken, ct);

        var result = new List<CoreShift>();
        foreach (var shift in shifts)
        {
            var firstActive = shift.FromDate > from ? shift.FromDate : from;
            var lastActive = shift.UntilDate ?? until;
            if (lastActive > until)
            {
                lastActive = until;
            }

            var slotCount = ShiftStaffingDemand.PerDay(shift.Quantity, shift.SumEmployees);
            var locationContext = (shift.RootId ?? shift.OriginalId ?? shift.Id).ToString();

            for (var date = firstActive; date <= lastActive; date = date.AddDays(1))
            {
                if (!IsShiftActiveOnWeekday(shift, date.DayOfWeek) || containerChildDays.Contains((shift.Id, date)))
                {
                    continue;
                }

                for (var i = 0; i < slotCount; i++)
                {
                    result.Add(new CoreShift(
                        Id: shift.Id.ToString(),
                        Name: string.IsNullOrWhiteSpace(shift.Name) ? shift.Abbreviation : shift.Name,
                        Date: date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
                        StartTime: shift.StartShift.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
                        EndTime: shift.EndShift.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
                        Hours: (double)shift.WorkTime,
                        RequiredAssignments: SingleSeat,
                        Priority: 0)
                    {
                        LocationContext = locationContext,
                    });
                }
            }
        }

        return result;
    }

    private async Task<HashSet<(Guid ShiftId, DateOnly Date)>> LoadContainerChildDaysAsync(
        DateOnly from, DateOnly until, Guid? analyseToken, CancellationToken ct)
    {
        var rows = await _shiftScheduleReader.GetShiftScheduleListAsync(
            from, until, holidayDates: null, visibleGroupIds: null, showUngroupedShifts: false, analyseToken, ct);
        return rows
            .Where(row => row.IsInTemplateContainer)
            .Select(row => (row.ShiftId, row.Date))
            .ToHashSet();
    }

    private static bool IsShiftActiveOnWeekday(Shift shift, DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => shift.IsMonday,
        DayOfWeek.Tuesday => shift.IsTuesday,
        DayOfWeek.Wednesday => shift.IsWednesday,
        DayOfWeek.Thursday => shift.IsThursday,
        DayOfWeek.Friday => shift.IsFriday,
        DayOfWeek.Saturday => shift.IsSaturday,
        DayOfWeek.Sunday => shift.IsSunday,
        _ => false,
    };
}
