// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Hours of a work entry as the order/accounting exports book them. Only the hour-bearing changes
/// (CorrectionWorkChangeTypes.HourBearing: corrections and travel) add to the work; replacements only move the
/// hours to another employee and briefing/debriefing carry no hours, so neither adds to the order. Same rule as
/// the period hours (PeriodHoursService) for the work's own employee.
/// </summary>
/// <param name="work">The exported work entry with its changes</param>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

internal static class OrderWorkHours
{
    /// <summary>
    /// Work time plus the change time of the hour-bearing changes, without surcharges.
    /// </summary>
    public static decimal WorkHours(WorkExportEntry work)
    {
        return work.WorkTime + HourBearingChanges(work).Sum(c => c.ChangeTime);
    }

    /// <summary>
    /// Work time and surcharges plus change time and surcharges of the hour-bearing changes.
    /// </summary>
    public static decimal BillableHours(WorkExportEntry work)
    {
        return work.WorkTime + work.Surcharges + HourBearingChanges(work).Sum(c => c.ChangeTime + c.Surcharges);
    }

    private static IEnumerable<WorkChangeExportEntry> HourBearingChanges(WorkExportEntry work)
    {
        return work.Changes.Where(c => CorrectionWorkChangeTypes.IsHourBearing(c.Type));
    }
}
