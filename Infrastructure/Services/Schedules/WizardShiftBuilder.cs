// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Models;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="IWizardShiftBuilder"/>.
/// Shifts are filtered by date range and weekday flag. Each (shift, date) tuple becomes a single CoreShift.
/// </summary>
/// <param name="context">EF Core database context</param>
public sealed class WizardShiftBuilder : IWizardShiftBuilder
{
    private readonly DataBaseContext _context;

    public WizardShiftBuilder(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CoreShift>> BuildAsync(
        IReadOnlyList<Guid>? shiftIds,
        DateOnly from,
        DateOnly until,
        CancellationToken ct)
    {
        var query = _context.Shift
            .AsNoTracking()
            .Where(s => s.FromDate <= until && (s.UntilDate == null || s.UntilDate >= from));

        if (shiftIds is { Count: > 0 })
        {
            var ids = shiftIds.ToList();
            query = query.Where(s => ids.Contains(s.Id));
        }

        var shifts = await query.ToListAsync(ct);

        var result = new List<CoreShift>();
        foreach (var shift in shifts)
        {
            var firstActive = shift.FromDate > from ? shift.FromDate : from;
            var lastActive = shift.UntilDate ?? until;
            if (lastActive > until)
            {
                lastActive = until;
            }

            var slotCount = shift.Quantity > 0 ? shift.Quantity : 1;

            for (var date = firstActive; date <= lastActive; date = date.AddDays(1))
            {
                if (!IsShiftActiveOnWeekday(shift, date.DayOfWeek))
                {
                    continue;
                }

                for (var i = 0; i < slotCount; i++)
                {
                    result.Add(new CoreShift(
                        Id: shift.Id.ToString(),
                        Name: string.IsNullOrWhiteSpace(shift.Name) ? shift.Abbreviation : shift.Name,
                        Date: date.ToString("yyyy-MM-dd"),
                        StartTime: shift.StartShift.ToString("HH:mm"),
                        EndTime: shift.EndShift.ToString("HH:mm"),
                        Hours: (double)shift.WorkTime,
                        RequiredAssignments: 1,
                        Priority: 0));
                }
            }
        }

        return result;
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
