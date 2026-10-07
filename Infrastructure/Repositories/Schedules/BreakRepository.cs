// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class BreakRepository : BaseRepository<Break>, IBreakRepository
{
    private readonly DataBaseContext _context;

    public BreakRepository(
        DataBaseContext context,
        ILogger<Break> logger)
        : base(context, logger)
    {
        _context = context;
    }

    public async Task<List<Guid>> GetClientIdsWithBreakOnDate(IReadOnlyCollection<Guid> clientIds, DateOnly date, Guid absenceId, Guid? analyseToken = null, CancellationToken cancellationToken = default)
    {
        if (clientIds.Count == 0)
        {
            return new List<Guid>();
        }

        var ids = clientIds.ToList();
        return await _context.Break
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.AnalyseToken == analyseToken && b.CurrentDate == date
                && b.AbsenceId == absenceId && ids.Contains(b.ClientId))
            .Select(b => b.ClientId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingAbsenceOfSameTypeAsync(Guid clientId, DateOnly date, Guid absenceId, TimeOnly startTime, TimeOnly endTime, Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .AsNoTracking()
            .AnyAsync(b => !b.IsDeleted
                && b.ClientId == clientId
                && b.CurrentDate == date
                && b.AbsenceId == absenceId
                && b.AnalyseToken == analyseToken
                && b.StartTime < endTime
                && startTime < b.EndTime,
                cancellationToken);
    }

    public async Task<List<Break>> GetByClientAndDateRangeAsync(Guid clientId, DateOnly fromDate, DateOnly untilDate, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.AnalyseToken == null && b.ClientId == clientId
                && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate)
            .OrderBy(b => b.CurrentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.AnalyseToken == null && b.CurrentDate == date && b.LockLevel < level)
            .WhereReopenableByGroup(_context, groupId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, level)
                .SetProperty(b => b.SealedAt, DateTime.UtcNow)
                .SetProperty(b => b.SealedBy, sealedBy)
                .SetProperty(b => b.SealedByGroupId, groupId), cancellationToken);
    }

    public async Task<int> UnsealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.AnalyseToken == null && b.CurrentDate == date && b.LockLevel == level)
            .WhereReopenableByGroup(_context, groupId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, WorkLockLevel.None)
                .SetProperty(b => b.SealedAt, (DateTime?)null)
                .SetProperty(b => b.SealedBy, (string?)null)
                .SetProperty(b => b.SealedByGroupId, (Guid?)null), cancellationToken);
    }

    public Task<int> SealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default)
    {
        return PeriodSealUpdates.SealAsync(
            MainScheduleBreaksIn(startDate, endDate),
            level,
            sealedBy,
            cancellationToken,
            s => s
                .SetProperty(b => b.PreSealSealedByGroupId, b => b.SealedByGroupId)
                .SetProperty(b => b.SealedByGroupId, (Guid?)null));
    }

    public Task<PeriodUnsealCounts> UnsealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, CancellationToken cancellationToken = default)
    {
        return PeriodSealUpdates.UnsealAsync(MainScheduleBreaksIn(startDate, endDate), level, cancellationToken, RestoreSealingGroup(level));
    }

    public async Task<int> SealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default)
    {
        var groupBreaks = await GroupBreaksInAsync(startDate, endDate, groupId, cancellationToken);
        return await PeriodSealUpdates.SealAsync(
            groupBreaks,
            level,
            sealedBy,
            cancellationToken,
            s => s
                .SetProperty(b => b.PreSealSealedByGroupId, b => b.SealedByGroupId)
                .SetProperty(b => b.SealedByGroupId, groupId));
    }

    public async Task<PeriodUnsealCounts> UnsealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default)
    {
        var reopenableBreaks = MainScheduleBreaksIn(startDate, endDate)
            .WhereReopenableByGroup(_context, groupId)
            .Where(b => b.LockLevel == level);

        var candidates = await reopenableBreaks
            .AsNoTracking()
            .Select(b => new { b.Id, b.ClientId, b.CurrentDate })
            .ToListAsync(cancellationToken);

        var stillLocked = await DayLockAttribution.LoadLockingSealsAsync(
            _context,
            candidates.Select(c => (c.CurrentDate, c.ClientId)).Distinct().ToList(),
            groupId,
            cancellationToken,
            level);

        var retainedByNewOwner = candidates
            .Where(c => stillLocked.ContainsKey((c.CurrentDate, c.ClientId)))
            .GroupBy(c => stillLocked[(c.CurrentDate, c.ClientId)].FirstOrDefault(owner => owner != null))
            .ToList();

        foreach (var retained in retainedByNewOwner)
        {
            var retainedIds = retained.Select(c => c.Id).ToList();
            var newOwner = retained.Key;
            await _context.Break
                .Where(b => retainedIds.Contains(b.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.SealedByGroupId, newOwner), cancellationToken);
        }

        var retainedBreakIds = retainedByNewOwner.SelectMany(g => g.Select(c => c.Id)).ToList();

        return await PeriodSealUpdates.UnsealAsync(
            reopenableBreaks.Where(b => !retainedBreakIds.Contains(b.Id)),
            level,
            cancellationToken,
            RestoreSealingGroup(level));
    }

    /// <summary>
    /// The unseal counterpart of the seal's owner record: an entry whose recorded pre-seal level is restored also gets
    /// its pre-seal owner back (the group that approved the day keeps its approval); every other entry loses its owner.
    /// </summary>
    private static Action<UpdateSettersBuilder<Break>> RestoreSealingGroup(WorkLockLevel level)
    {
        return setters => setters
            .SetProperty(
                b => b.SealedByGroupId,
                b => b.PreSealLockLevel != null && b.PreSealLockLevel < level ? b.PreSealSealedByGroupId : null)
            .SetProperty(b => b.PreSealSealedByGroupId, (Guid?)null);
    }

    private IQueryable<Break> MainScheduleBreaksIn(DateOnly startDate, DateOnly endDate)
    {
        return _context.Break
            .Where(b => !b.IsDeleted && b.AnalyseToken == null && b.CurrentDate >= startDate && b.CurrentDate <= endDate);
    }

    private async Task<IQueryable<Break>> GroupBreaksInAsync(DateOnly startDate, DateOnly endDate, Guid groupId, CancellationToken cancellationToken)
    {
        var memberBreakIds = await GroupBreakScope.LoadMemberBreakIdsAsync(_context, groupId, startDate, endDate, cancellationToken);

        return MainScheduleBreaksIn(startDate, endDate)
            .WhereAttributedToGroup(_context, groupId, memberBreakIds);
    }

    public async Task<List<(DateOnly Date, int Total, int Sealed)>> GetSealingSummaryAsync(DateOnly from, DateOnly to, Guid? groupId, CancellationToken cancellationToken = default)
    {
        var query = groupId.HasValue
            ? await GroupBreaksInAsync(from, to, groupId.Value, cancellationToken)
            : MainScheduleBreaksIn(from, to);

        query = query.AsNoTracking();

        var grouped = await query
            .GroupBy(b => b.CurrentDate)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Count(),
                Sealed = g.Count(b => b.LockLevel == WorkLockLevel.Closed)
            })
            .ToListAsync(cancellationToken);

        return grouped.Select(x => (x.Date, x.Total, x.Sealed)).ToList();
    }
}
