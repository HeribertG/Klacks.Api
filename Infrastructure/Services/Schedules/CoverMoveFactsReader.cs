// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the facts of works for the cover decision. Replacements come from ReplacementCoverQuery, the single
/// source of what counts as one.
/// </summary>
/// <param name="context">Database context of the caller (soft-delete filters apply)</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class CoverMoveFactsReader : ICoverMoveFactsReader
{
    private readonly DataBaseContext _context;

    public CoverMoveFactsReader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<bool> IsFreeAsync(
        Guid analyseToken, Guid clientId, DateTime startAt, DateTime endAt, CancellationToken cancellationToken)
    {
        var firstDay = DateOnly.FromDateTime(startAt).AddDays(-1);
        var lastDay = DateOnly.FromDateTime(endAt);

        var works = await _context.Work.AsNoTracking()
            .Where(w => w.ClientId == clientId
                && w.ParentWorkId == null
                && w.CurrentDate >= firstDay
                && w.CurrentDate <= lastDay
                && (w.AnalyseToken == analyseToken
                    || (w.AnalyseToken == null
                        && !_context.Work.IgnoreQueryFilters().Any(clone => clone.AnalyseToken == analyseToken && clone.ClonedFromWorkId == w.Id))))
            .Select(w => new { w.CurrentDate, w.StartTime, w.EndTime })
            .ToListAsync(cancellationToken);
        if (works.Any(w =>
            {
                var (workStart, workEnd) = ReplacementWindow.ToInterval(w.CurrentDate, w.StartTime, w.StartTime, w.EndTime);
                return workStart < endAt && startAt < workEnd;
            }))
        {
            return false;
        }

        var absences = await (
                from b in _context.Break.AsNoTracking()
                join a in _context.Absence.AsNoTracking() on b.AbsenceId equals a.Id
                where b.ClientId == clientId
                    && b.ParentWorkId == null
                    && !a.IsOnCall
                    && b.CurrentDate >= firstDay
                    && b.CurrentDate <= lastDay
                    && (b.AnalyseToken == analyseToken || b.AnalyseToken == null)
                select new { b.CurrentDate, b.StartTime, b.EndTime })
            .ToListAsync(cancellationToken);
        return !absences.Any(b =>
        {
            var (absenceStart, absenceEnd) = AbsenceWorkCoverage.Window(b.CurrentDate, b.StartTime, b.EndTime);
            return absenceStart < endAt && startAt < absenceEnd;
        });
    }

    public async Task<IReadOnlyDictionary<Guid, CoverMoveFacts>> LoadAsync(
        IReadOnlyCollection<Guid> workIds, Guid? analyseToken, CancellationToken cancellationToken)
    {
        var ids = workIds.ToList();
        var works = await _context.Work.AsNoTracking()
            .Where(w => ids.Contains(w.Id) && w.AnalyseToken == analyseToken)
            .Select(w => new { w.Id, w.ClientId, w.ParentWorkId, w.CurrentDate, w.StartTime, w.EndTime, w.LockLevel, w.MovedFromClientId })
            .ToListAsync(cancellationToken);
        if (works.Count == 0)
        {
            return new Dictionary<Guid, CoverMoveFacts>();
        }

        var subWorks = await _context.Work.AsNoTracking()
            .Where(w => w.ParentWorkId != null && ids.Contains(w.ParentWorkId.Value))
            .Select(w => new { w.Id, ParentId = w.ParentWorkId!.Value, w.LockLevel })
            .ToListAsync(cancellationToken);
        var parentsWithLockedBreak = await _context.Break.AsNoTracking()
            .Where(b => b.ParentWorkId != null && ids.Contains(b.ParentWorkId.Value) && b.LockLevel != WorkLockLevel.None)
            .Select(b => b.ParentWorkId!.Value)
            .ToListAsync(cancellationToken);
        var replaced = await ReplacementCoverQuery.LoadCoveredWorkIdsAsync(
            _context, analyseToken, works.Min(w => w.CurrentDate), works.Max(w => w.CurrentDate), cancellationToken);

        var parentsWithLockedChild = subWorks
            .Where(sub => sub.LockLevel != WorkLockLevel.None)
            .Select(sub => sub.ParentId)
            .Concat(parentsWithLockedBreak)
            .ToHashSet();
        var parentsWithReplacement = subWorks
            .Where(sub => replaced.Contains(sub.Id))
            .Select(sub => sub.ParentId)
            .ToHashSet();

        return works.ToDictionary(
            w => w.Id,
            w => new CoverMoveFacts(
                w.Id, w.ClientId, w.ParentWorkId, w.CurrentDate, w.StartTime, w.EndTime, w.LockLevel,
                replaced.Contains(w.Id) || parentsWithReplacement.Contains(w.Id),
                parentsWithLockedChild.Contains(w.Id),
                w.MovedFromClientId != null));
    }
}
