// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.ScheduleEntries;

/// <summary>
/// Reads the materialised schedule rows from get_schedule_entries and applies the criteria's client, entry-type and
/// start-time narrowing in the same SQL statement.
/// </summary>
/// <param name="context">Database context the function is called on</param>
public class ScheduleEntriesService : IScheduleEntriesService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<ScheduleEntriesService> _logger;

    public ScheduleEntriesService(
        DataBaseContext context,
        ILogger<ScheduleEntriesService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScheduleCell>> GetScheduleEntriesAsync(
        ScheduleEntriesCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFunctionQuery(criteria.From, criteria.Until, criteria.VisibleGroupIds, criteria.AnalyseToken);

        if (criteria.ClientIds is not null)
        {
            var clientIds = criteria.ClientIds.ToList();
            query = query.Where(e => clientIds.Contains(e.ClientId));
        }

        if (criteria.EntryType is { } entryType)
        {
            var entryTypeValue = (int)entryType;
            query = query.Where(e => e.EntryType == entryTypeValue);
        }

        if (criteria.OrderByStartTime)
        {
            query = query.OrderBy(e => e.StartTime);
        }

        return await query.ToListAsync(cancellationToken);
    }

    private IQueryable<ScheduleCell> BuildFunctionQuery(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyCollection<Guid>? visibleGroupIds,
        Guid? analyseToken)
    {
        _logger.LogDebug(
            "Building schedule entries query from {StartDate} to {EndDate}, VisibleGroups: {VisibleGroupCount}, AnalyseToken: {AnalyseToken}",
            startDate,
            endDate,
            visibleGroupIds?.Count ?? 0,
            analyseToken);

        var startDateTime = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDateTime = DateTime.SpecifyKind(endDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var visibleGroupArray = visibleGroupIds?.ToArray() ?? [];

        if (analyseToken.HasValue)
        {
            return _context.ScheduleCells
                .FromSqlInterpolated($@"
                    SELECT * FROM get_schedule_entries(
                        {startDateTime}::DATE,
                        {endDateTime}::DATE,
                        {visibleGroupArray}::UUID[],
                        {analyseToken.Value}::UUID
                    )");
        }

        return _context.ScheduleCells
            .FromSqlInterpolated($@"
                SELECT * FROM get_schedule_entries(
                    {startDateTime}::DATE,
                    {endDateTime}::DATE,
                    {visibleGroupArray}::UUID[]
                )");
    }
}
