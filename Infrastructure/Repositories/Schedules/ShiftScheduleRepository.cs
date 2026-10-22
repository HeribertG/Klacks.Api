// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the shift schedule for the Schedule view, restricted to the shifts of the groups the caller may see
/// (group visibility is a security boundary; a restricted caller without a visible group gets no grouped shift).
/// </summary>
/// <param name="shiftScheduleService">Executes the shift schedule stored procedures</param>
/// <param name="shiftGroupFilterService">Resolves the caller's group restriction (null = unrestricted)</param>
/// <param name="shiftScheduleFilterService">Applies the UI filters to the schedule query</param>
/// <param name="logger">Diagnostics logging</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Microsoft.EntityFrameworkCore;
using Klacks.Api.Infrastructure.Services.ShiftSchedule;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ShiftScheduleRepository : IShiftScheduleRepository
{
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IShiftGroupFilterService _shiftGroupFilterService;
    private readonly IShiftScheduleFilterService _shiftScheduleFilterService;
    private readonly ILogger<ShiftScheduleRepository> _logger;

    public ShiftScheduleRepository(
        IShiftScheduleService shiftScheduleService,
        IShiftGroupFilterService shiftGroupFilterService,
        IShiftScheduleFilterService shiftScheduleFilterService,
        ILogger<ShiftScheduleRepository> logger)
    {
        _shiftScheduleService = shiftScheduleService;
        _shiftGroupFilterService = shiftGroupFilterService;
        _shiftScheduleFilterService = shiftScheduleFilterService;
        _logger = logger;
    }

    public async Task<(List<ShiftDayAssignment> Shifts, int TotalCount)> GetShiftScheduleAsync(
        ShiftScheduleFilter filter,
        CancellationToken cancellationToken)
    {
        var startDate = filter.StartDate;
        var endDate = filter.EndDate;

        if (startDate == DateOnly.MinValue || endDate == DateOnly.MinValue)
        {
            _logger.LogWarning("Invalid dates received - using current month as fallback");
            var now = DateTime.UtcNow;
            startDate = new DateOnly(now.Year, now.Month, 1).AddDays(-10);
            endDate = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)).AddDays(10);
        }

        _logger.LogInformation(
            "Fetching shift schedule for {StartDate} to {EndDate}",
            startDate,
            endDate);

        var holidayDates = filter.HolidayDates?
            .Select(d => DateOnly.FromDateTime(d))
            .ToList();

        var visibleGroupIds = await _shiftGroupFilterService.ResolveShiftScheduleGroupIdsAsync(filter.SelectedGroup);

        var query = _shiftScheduleService.GetShiftScheduleQuery(
            startDate,
            endDate,
            holidayDates,
            visibleGroupIds,
            filter.ShowUngroupedShifts,
            filter.AnalyseToken);

        query = _shiftScheduleFilterService.ApplyAllFilters(query, filter);

        var allAssignments = await query.ToListAsync(cancellationToken);

        var uniqueShiftIds = allAssignments
            .GroupBy(s => s.ShiftId)
            .Select(g => new { ShiftId = g.Key, g.First().ShiftName })
            .OrderBy(x => x.ShiftName)
            .Select(x => x.ShiftId)
            .ToList();

        var totalCount = uniqueShiftIds.Count;

        var paginatedShiftIds = uniqueShiftIds
            .Skip(filter.StartRow)
            .Take(filter.RowCount)
            .ToHashSet();

        var result = allAssignments
            .Where(s => paginatedShiftIds.Contains(s.ShiftId))
            .ToList();

        await AttachRequiredQualificationsAsync(result, cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} shift schedule entries for {ShiftCount} of {TotalCount} shifts",
            result.Count,
            paginatedShiftIds.Count,
            totalCount);

        return (result, totalCount);
    }

    public async Task<List<ShiftDayAssignment>> GetShiftSchedulePartialAsync(
        ShiftSchedulePartialFilter filter,
        CancellationToken cancellationToken)
    {
        if (filter.ShiftDatePairs.Count == 0)
        {
            return [];
        }

        _logger.LogInformation(
            "Fetching partial shift schedule for {Count} shift/date pairs",
            filter.ShiftDatePairs.Count);

        var shiftDatePairs = filter.ShiftDatePairs
            .Select(p => (p.ShiftId, DateOnly.FromDateTime(p.Date)))
            .ToList();

        var visibleGroupIds = await _shiftGroupFilterService.ResolveShiftScheduleGroupIdsAsync(null);

        var assignments = await _shiftScheduleService.GetShiftSchedulePartialAsync(
            shiftDatePairs,
            filter.AnalyseToken,
            visibleGroupIds,
            cancellationToken);

        await AttachRequiredQualificationsAsync(assignments, cancellationToken);

        return assignments;
    }

    private async Task AttachRequiredQualificationsAsync(
        List<ShiftDayAssignment> assignments,
        CancellationToken cancellationToken)
    {
        if (assignments.Count == 0)
        {
            return;
        }

        var shiftIds = assignments
            .Select(a => a.ShiftId)
            .Distinct()
            .ToList();

        var qualificationsByShift = await _shiftScheduleService.GetRequiredQualificationsByShiftAsync(
            shiftIds,
            cancellationToken);

        foreach (var assignment in assignments)
        {
            assignment.RequiredQualifications =
                qualificationsByShift.TryGetValue(assignment.ShiftId, out var qualifications)
                    ? qualifications
                    : new List<ShiftRequiredQualification>();
        }
    }
}
