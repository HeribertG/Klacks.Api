// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.ShiftSchedule;

/// <summary>
/// IShiftScheduleReader plus the composable get_shift_schedule query, for Infrastructure callers that add their own
/// filters, sorting and paging in the database (ShiftScheduleRepository). Application code reads through
/// IShiftScheduleReader and never sees the IQueryable.
/// </summary>
public interface IShiftScheduleService : IShiftScheduleReader
{
    IQueryable<ShiftDayAssignment> GetShiftScheduleQuery(
        DateOnly startDate,
        DateOnly endDate,
        List<DateOnly>? holidayDates = null,
        List<Guid>? visibleGroupIds = null,
        bool showUngroupedShifts = false,
        Guid? analyseToken = null);
}
