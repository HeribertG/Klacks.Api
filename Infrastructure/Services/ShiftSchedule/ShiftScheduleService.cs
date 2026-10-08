// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the shift schedule (stored procedures get_shift_schedule / get_shift_schedule_partial) and the required
/// qualifications shown on it. The qualifications are the rows that apply to each shift, inherited along the order
/// tree when a cut piece carries none of its own (optional rows included, it is a display list).
/// </summary>
/// <param name="context">EF Core context for the stored procedure calls</param>
/// <param name="requirementRepository">Resolves the required qualifications that apply to each shift</param>
/// <param name="logger">Debug logging of the schedule queries</param>

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Klacks.Api.Infrastructure.Services.ShiftSchedule;

public class ShiftScheduleService : IShiftScheduleService
{
    private readonly DataBaseContext _context;
    private readonly IShiftRequiredQualificationRepository _requirementRepository;
    private readonly ILogger<ShiftScheduleService> _logger;

    public ShiftScheduleService(
        DataBaseContext context,
        IShiftRequiredQualificationRepository requirementRepository,
        ILogger<ShiftScheduleService> logger)
    {
        _context = context;
        _requirementRepository = requirementRepository;
        _logger = logger;
    }

    public async Task<Dictionary<Guid, List<ShiftRequiredQualification>>> GetRequiredQualificationsByShiftAsync(
        IReadOnlyCollection<Guid> shiftIds,
        CancellationToken cancellationToken = default)
    {
        if (shiftIds.Count == 0)
        {
            return new Dictionary<Guid, List<ShiftRequiredQualification>>();
        }

        var effective = await _requirementRepository.GetEffectiveByShiftIdsAsync(shiftIds, cancellationToken);

        return effective
            .GroupBy(e => e.ShiftId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Requirement).ToList());
    }

    public IQueryable<ShiftDayAssignment> GetShiftScheduleQuery(
        DateOnly startDate,
        DateOnly endDate,
        List<DateOnly>? holidayDates = null,
        List<Guid>? visibleGroupIds = null,
        bool showUngroupedShifts = false,
        Guid? analyseToken = null)
    {
        _logger.LogDebug(
            "Building shift schedule query from {StartDate} to {EndDate} with {HolidayCount} holidays, VisibleGroups: {VisibleGroupCount}, ShowUngrouped: {ShowUngrouped}, AnalyseToken: {AnalyseToken}",
            startDate,
            endDate,
            holidayDates?.Count ?? 0,
            visibleGroupIds?.Count ?? 0,
            showUngroupedShifts,
            analyseToken);

        var holidays = holidayDates ?? [];
        var holidayArray = holidays
            .Select(h => DateTime.SpecifyKind(h.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc))
            .ToArray();

        var startDateTime = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDateTime = DateTime.SpecifyKind(endDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var visibleGroupArray = visibleGroupIds?.ToArray() ?? [];

        if (analyseToken.HasValue)
        {
            return _context.ShiftDayAssignments
                .FromSqlInterpolated($@"
                    SELECT * FROM get_shift_schedule(
                        {startDateTime}::DATE,
                        {endDateTime}::DATE,
                        {holidayArray}::DATE[],
                        {visibleGroupArray}::UUID[],
                        {showUngroupedShifts},
                        {analyseToken.Value}::UUID
                    )");
        }

        return _context.ShiftDayAssignments
            .FromSqlInterpolated($@"
                SELECT * FROM get_shift_schedule(
                    {startDateTime}::DATE,
                    {endDateTime}::DATE,
                    {holidayArray}::DATE[],
                    {visibleGroupArray}::UUID[],
                    {showUngroupedShifts}
                )");
    }

    public async Task<List<ShiftDayAssignment>> GetShiftSchedulePartialAsync(
        List<(Guid ShiftId, DateOnly Date)> shiftDatePairs,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        if (shiftDatePairs.Count == 0)
        {
            return [];
        }

        _logger.LogDebug(
            "Fetching partial shift schedule for {Count} shift/date pairs, AnalyseToken: {AnalyseToken}",
            shiftDatePairs.Count,
            analyseToken);

        var shiftIds = shiftDatePairs.Select(p => p.ShiftId).ToArray();
        var dates = shiftDatePairs
            .Select(p => DateTime.SpecifyKind(p.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc))
            .ToArray();

        var shiftIdsParam = new NpgsqlParameter("shiftIds", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = shiftIds };
        var datesParam = new NpgsqlParameter("dates", NpgsqlDbType.Array | NpgsqlDbType.Date) { Value = dates };
        var tokenParam = new NpgsqlParameter("analyseToken", NpgsqlDbType.Uuid)
        {
            Value = (object?)analyseToken ?? DBNull.Value
        };

        const string sql = @"
            SELECT * FROM get_shift_schedule_partial(
                (SELECT array_agg(ROW(s, d)::shift_date_pair)
                 FROM unnest(@shiftIds::UUID[], @dates::DATE[]) AS t(s, d)),
                @analyseToken::UUID
            )";

        return await _context.ShiftDayAssignments
            .FromSqlRaw(sql, shiftIdsParam, datesParam, tokenParam)
            .ToListAsync(cancellationToken);
    }
}
