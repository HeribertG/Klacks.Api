// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IPlanningRuleDataReader"/>. GroupItem has no reliable soft-delete filter for these
/// joins, so IsDeleted is filtered explicitly; GroupItem.ValidFrom/ValidUntil are timestamps holding calendar
/// days, compared against UTC midnight bounds. Both queries filter on the entity and project last - the
/// shape proven against PostgreSQL (see ShiftGroupScopeReadRepository).
/// </summary>
/// <param name="context">Database context holding GroupItem and Work</param>

using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class PlanningRuleDataReader : IPlanningRuleDataReader
{
    private readonly DataBaseContext _context;

    public PlanningRuleDataReader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<PlanningRuleGroupMembership>> GetGroupMembershipsAsync(
        IReadOnlyCollection<Guid> groupIds,
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken = default)
    {
        if (groupIds.Count == 0 || clientIds.Count == 0)
        {
            return [];
        }

        var groupIdList = groupIds.Distinct().ToList();
        var clientIdList = clientIds.Distinct().Select(id => (Guid?)id).ToList();
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var untilUtc = until.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var rows = await _context.GroupItem
            .AsNoTracking()
            .Where(item => !item.IsDeleted
                && item.ClientId != null
                && groupIdList.Contains(item.GroupId)
                && clientIdList.Contains(item.ClientId)
                && (item.AnalyseToken == null || (analyseToken != null && item.AnalyseToken == analyseToken))
                && (item.ValidFrom == null || item.ValidFrom <= untilUtc)
                && (item.ValidUntil == null || item.ValidUntil >= fromUtc))
            .Select(item => new { item.GroupId, ClientId = item.ClientId!.Value })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows.Select(row => new PlanningRuleGroupMembership(row.GroupId, row.ClientId)).ToList();
    }

    public async Task<List<PlanningRuleWorkSpan>> GetWorkSpansAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken = default)
    {
        if (clientIds.Count == 0 || until < from)
        {
            return [];
        }

        var clientIdList = clientIds.Distinct().ToList();
        var rows = await _context.Work
            .AsNoTracking()
            .Where(work => !work.IsDeleted
                && clientIdList.Contains(work.ClientId)
                && work.AnalyseToken == analyseToken
                && work.CurrentDate >= from
                && work.CurrentDate <= until)
            .OrderBy(work => work.ClientId)
            .ThenBy(work => work.CurrentDate)
            .ThenBy(work => work.StartTime)
            .Select(work => new { work.Id, work.ClientId, work.CurrentDate, work.StartTime, work.EndTime, work.WorkTime })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new PlanningRuleWorkSpan(row.ClientId, row.CurrentDate, row.StartTime, row.EndTime, row.WorkTime, row.Id))
            .ToList();
    }
}
