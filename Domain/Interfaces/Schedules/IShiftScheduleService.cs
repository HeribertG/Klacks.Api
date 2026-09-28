// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftScheduleService
{
    Task<Dictionary<Guid, List<ShiftRequiredQualification>>> GetRequiredQualificationsByShiftAsync(
        IReadOnlyCollection<Guid> shiftIds,
        CancellationToken cancellationToken = default);

    IQueryable<ShiftDayAssignment> GetShiftScheduleQuery(
        DateOnly startDate,
        DateOnly endDate,
        List<DateOnly>? holidayDates = null,
        List<Guid>? visibleGroupIds = null,
        bool showUngroupedShifts = false,
        Guid? analyseToken = null);

    Task<List<ShiftDayAssignment>> GetShiftSchedulePartialAsync(
        List<(Guid ShiftId, DateOnly Date)> shiftDatePairs,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);
}
