// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// When a service ends and whether it has ended: the end is the work's date plus its end time, one day later when the
/// working-time span crosses midnight (night shifts, and equal bounds as a 24 hour duty). Sealing confirms that the
/// service took place, so it is only allowed from this instant on. The in-memory check and the database filter
/// express one rule and are pinned against each other by a parity test; both compare against the company-local
/// wall clock, never UTC or the browser.
/// </summary>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.ValueObjects;

namespace Klacks.Api.Domain.Services.Schedules;

public static class WorkServiceEnd
{
    private const int NextDayOffset = 1;
    private const int PreviousDayOffset = -1;

    public static DateTime EndOf(Work work) => EndOf(work.CurrentDate, work.StartTime, work.EndTime);

    public static DateTime EndOf(DateOnly date, TimeOnly start, TimeOnly end)
    {
        var endDate = TimeRange.ForWorkingTime(start, end).CrossesMidnight ? date.AddDays(NextDayOffset) : date;
        return endDate.ToDateTime(end);
    }

    public static bool HasEnded(Work work, DateTime companyNow) => EndOf(work) <= companyNow;

    public static Expression<Func<Work, bool>> EndedBy(ServiceEndCutoff cutoff)
    {
        var today = cutoff.Today;
        var yesterday = today.AddDays(PreviousDayOffset);
        var time = cutoff.Time;

        return w =>
            (w.EndTime > w.StartTime && (w.CurrentDate < today || (w.CurrentDate == today && w.EndTime <= time)))
            || (w.EndTime <= w.StartTime && (w.CurrentDate < yesterday || (w.CurrentDate == yesterday && w.EndTime <= time)));
    }

    public static Expression<Func<Work, bool>> NotEndedBy(ServiceEndCutoff cutoff)
    {
        var ended = EndedBy(cutoff);
        return Expression.Lambda<Func<Work, bool>>(Expression.Not(ended.Body), ended.Parameters);
    }
}
