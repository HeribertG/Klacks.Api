// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-side repository for period-closing lookups: group/user display names and schedule-note issues.
/// </summary>
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class PeriodClosingReadRepository : IPeriodClosingReadRepository
{
    private readonly DataBaseContext _context;

    public PeriodClosingReadRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<Guid, string>> GetGroupNames(IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken)
    {
        if (groupIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await _context.Group
            .AsNoTracking()
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken);
    }

    public async Task<Dictionary<string, string>> GetUserDisplayNames(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        return await _context.AppUser
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim(), cancellationToken);
    }

    public async Task<List<PeriodScheduleNoteIssueRow>> GetScheduleNoteIssues(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? groupId,
        int maxNotes,
        CancellationToken cancellationToken)
    {
        var query = from note in _context.ScheduleNotes.AsNoTracking()
                    where !note.IsDeleted
                          && note.AnalyseToken == null
                          && note.CurrentDate >= fromDate
                          && note.CurrentDate <= toDate
                    join client in _context.Client.AsNoTracking() on note.ClientId equals client.Id
                    where !client.IsDeleted
                    select new { note, client };

        if (groupId.HasValue)
        {
            var resolvedGroupId = groupId.Value;
            query = query.Where(x =>
                _context.GroupItem.AsNoTracking()
                    .Any(gi => !gi.IsDeleted && gi.ClientId == x.client.Id && gi.GroupId == resolvedGroupId));
        }

        var rows = await query
            .OrderBy(x => x.note.CurrentDate)
            .ThenBy(x => x.client.FirstName)
            .ThenBy(x => x.client.Name)
            .Take(maxNotes)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new PeriodScheduleNoteIssueRow(
            r.note.CurrentDate,
            r.client.Id,
            r.client.FirstName,
            r.client.Name,
            r.note.Content)).ToList();
    }
}
