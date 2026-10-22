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

    /// <summary>
    /// The rows of get_shift_schedule for the period, materialised in the function's own order: the same call as
    /// GetShiftScheduleQuery, for callers that use every row and add no filter of their own.
    /// </summary>
    /// <param name="startDate">First day of the period, inclusive</param>
    /// <param name="endDate">Last day of the period, inclusive</param>
    /// <param name="holidayDates">Holidays of the period; null = none</param>
    /// <param name="visibleGroupIds">null = unrestricted; a list (even an empty one) restricts to shifts of those groups
    /// and their subgroups</param>
    /// <param name="showUngroupedShifts">Whether shifts without any group are added</param>
    /// <param name="analyseToken">Scenario to read; null = the real plan</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<IReadOnlyList<ShiftDayAssignment>> GetShiftScheduleListAsync(
        DateOnly startDate,
        DateOnly endDate,
        List<DateOnly>? holidayDates = null,
        List<Guid>? visibleGroupIds = null,
        bool showUngroupedShifts = false,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);

    Task<List<ShiftDayAssignment>> GetShiftSchedulePartialAsync(
        List<(Guid ShiftId, DateOnly Date)> shiftDatePairs,
        Guid? analyseToken = null,
        List<Guid>? visibleGroupIds = null,
        CancellationToken cancellationToken = default);
}
