// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core-backed implementation of AnalyseScenario data operations.
/// Owns the DataBaseContext-level bulk operations that command handlers
/// used to perform inline; extracted so Create / Accept / Reject / Delete
/// handlers share one source of truth for cloning, promoting, and the
/// soft-delete + orphan sweep.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public class AnalyseScenarioService : IAnalyseScenarioService
{
    private readonly DataBaseContext _context;

    public AnalyseScenarioService(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> GetGroupHierarchyIdsAsync(Guid groupId, CancellationToken ct)
    {
        var allGroups = await _context.Group
            .Where(g => !g.IsDeleted)
            .Select(g => new { g.Id, g.Parent })
            .ToListAsync(ct);

        var result = new List<Guid> { groupId };
        var queue = new Queue<Guid>();
        queue.Enqueue(groupId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            foreach (var childId in allGroups.Where(g => g.Parent == currentId).Select(g => g.Id))
            {
                result.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return result;
    }

    public async Task SoftDeleteScenarioDataAsync(Guid token, CancellationToken ct)
    {
        var works = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && !w.IsDeleted).ToListAsync(ct);
        var workIds = works.Select(w => w.Id).ToList();

        var orphanSubWorks = workIds.Count == 0
            ? new List<Work>()
            : await _context.Work.IgnoreQueryFilters()
                .Where(w => !w.IsDeleted
                    && w.ParentWorkId.HasValue
                    && workIds.Contains(w.ParentWorkId.Value))
                .ToListAsync(ct);
        var allWorkIds = workIds.Concat(orphanSubWorks.Select(w => w.Id)).ToList();

        var workChanges = await _context.WorkChange
            .Where(wc => !wc.IsDeleted && allWorkIds.Contains(wc.WorkId)).ToListAsync(ct);
        foreach (var wc in workChanges) { wc.IsDeleted = true; wc.DeletedTime = DateTime.UtcNow; }

        var expenses = await _context.Expenses
            .Where(e => !e.IsDeleted && allWorkIds.Contains(e.WorkId)).ToListAsync(ct);
        foreach (var exp in expenses) { exp.IsDeleted = true; exp.DeletedTime = DateTime.UtcNow; }

        foreach (var w in works) { w.IsDeleted = true; w.DeletedTime = DateTime.UtcNow; }
        foreach (var w in orphanSubWorks) { w.IsDeleted = true; w.DeletedTime = DateTime.UtcNow; }

        var breaks = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted).ToListAsync(ct);
        foreach (var b in breaks) { b.IsDeleted = true; b.DeletedTime = DateTime.UtcNow; }

        var orphanSubBreaks = workIds.Count == 0
            ? new List<Break>()
            : await _context.Break.IgnoreQueryFilters()
                .Where(b => !b.IsDeleted
                    && b.ParentWorkId.HasValue
                    && workIds.Contains(b.ParentWorkId.Value))
                .ToListAsync(ct);
        foreach (var b in orphanSubBreaks) { b.IsDeleted = true; b.DeletedTime = DateTime.UtcNow; }

        var shifts = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted).ToListAsync(ct);
        foreach (var s in shifts) { s.IsDeleted = true; s.DeletedTime = DateTime.UtcNow; }

        var notes = await _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == token && !sn.IsDeleted).ToListAsync(ct);
        foreach (var sn in notes) { sn.IsDeleted = true; sn.DeletedTime = DateTime.UtcNow; }

        var softenings = await _context.WorkSoftening.IgnoreQueryFilters()
            .Where(ws => ws.AnalyseToken == token && !ws.IsDeleted).ToListAsync(ct);
        foreach (var ws in softenings) { ws.IsDeleted = true; ws.DeletedTime = DateTime.UtcNow; }

        var preferences = await _context.Set<ClientShiftPreference>().IgnoreQueryFilters()
            .Where(p => p.AnalyseToken == token && !p.IsDeleted).ToListAsync(ct);
        foreach (var p in preferences) { p.IsDeleted = true; p.DeletedTime = DateTime.UtcNow; }

        var expenses2 = await _context.Set<ShiftExpenses>().IgnoreQueryFilters()
            .Where(e => e.AnalyseToken == token && !e.IsDeleted).ToListAsync(ct);
        foreach (var e in expenses2) { e.IsDeleted = true; e.DeletedTime = DateTime.UtcNow; }
    }

    public async Task<Dictionary<Guid, Guid>> CloneScenarioDataAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, Guid token, IReadOnlyCollection<Guid>? additionalShiftIds, CancellationToken ct)
    {
        var groupIds = groupId.HasValue
            ? await GetGroupHierarchyIdsAsync(groupId.Value, ct)
            : null;

        var boundaryFrom = fromDate.AddDays(-ScenarioConstants.BoundaryDays);
        var boundaryUntil = untilDate.AddDays(ScenarioConstants.BoundaryDays);

        var shiftIdMap = await CloneShifts(groupIds, additionalShiftIds, token, ct);
        var workIdMap = await CloneWorks(groupIds, boundaryFrom, boundaryUntil, token, shiftIdMap, ct);
        await CloneBreaks(groupIds, boundaryFrom, boundaryUntil, token, workIdMap, ct);
        await CloneScheduleNotes(groupIds, boundaryFrom, boundaryUntil, token, ct);
        await CloneClientShiftPreferences(shiftIdMap, token, ct);
        await CloneShiftExpenses(shiftIdMap, token, ct);

        return shiftIdMap;
    }

    public async Task SoftDeleteRealScheduleDataAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        List<Guid>? shiftIds = null;
        List<Guid>? clientIds = null;

        if (groupId.HasValue)
        {
            var groupIds = await GetGroupHierarchyIdsAsync(groupId.Value, ct);

            shiftIds = await _context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ShiftId != null)
                .Select(gi => gi.ShiftId!.Value)
                .Distinct()
                .ToListAsync(ct);

            clientIds = await _context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ClientId != null)
                .Select(gi => gi.ClientId!.Value)
                .Distinct()
                .ToListAsync(ct);
        }

        await SoftDeleteRealWorks(shiftIds, fromDate, untilDate, ct);
        await SoftDeleteRealBreaks(clientIds, fromDate, untilDate, ct);
        await SoftDeleteRealScheduleNotes(clientIds, fromDate, untilDate, ct);
    }

    public async Task ValidateNoAcceptConflictsAsync(Guid token, CancellationToken ct)
    {
        var clones = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted && s.ScenarioSourceShiftId.HasValue)
            .Select(s => new { s.Id, SourceId = s.ScenarioSourceShiftId!.Value, s.SourceChildCountSnapshot })
            .ToListAsync(ct);

        if (clones.Count == 0) return;

        var sourceIds = clones.Select(c => c.SourceId).Distinct().ToList();

        var existingSources = await _context.Shift.IgnoreQueryFilters()
            .Where(s => sourceIds.Contains(s.Id))
            .Select(s => new { s.Id, s.IsDeleted })
            .ToListAsync(ct);

        var sourcesById = existingSources.ToDictionary(s => s.Id, s => s.IsDeleted);

        var missing = sourceIds.Where(id => !sourcesById.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw new ConflictException(
                $"Cannot accept scenario: source shifts no longer exist: {string.Join(", ", missing)}.");
        }

        var deleted = sourceIds.Where(id => sourcesById[id]).ToList();
        if (deleted.Count > 0)
        {
            throw new ConflictException(
                $"Cannot accept scenario: source shifts have been deleted: {string.Join(", ", deleted)}.");
        }

        var currentChildCounts = await _context.Shift.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted
                && s.AnalyseToken == null
                && s.ParentId.HasValue
                && sourceIds.Contains(s.ParentId.Value))
            .GroupBy(s => s.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParentId, x => x.Count, ct);

        foreach (var clone in clones)
        {
            var snapshot = clone.SourceChildCountSnapshot ?? 0;
            var current = currentChildCounts.GetValueOrDefault(clone.SourceId, 0);
            if (snapshot != current)
            {
                throw new ConflictException(
                    $"Cannot accept scenario: source shift {clone.SourceId} subtree changed after scenario creation (children snapshot={snapshot}, current={current}). Likely a concurrent cut by another user.");
            }
        }
    }

    public async Task PromoteScenarioWorksAsync(Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        var cloneShifts = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted && s.ScenarioSourceShiftId.HasValue)
            .Select(s => new { s.Id, SourceId = s.ScenarioSourceShiftId!.Value })
            .ToListAsync(ct);

        var cloneToSource = cloneShifts.ToDictionary(c => c.Id, c => c.SourceId);
        var now = DateTime.UtcNow;

        var scenarioWorks = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && !w.IsDeleted).ToListAsync(ct);
        var promotedWorkIds = new List<Guid>();
        var boundaryWorkIds = new List<Guid>();
        foreach (var w in scenarioWorks)
        {
            if (w.CurrentDate < fromDate || w.CurrentDate > untilDate)
            {
                w.IsDeleted = true;
                w.DeletedTime = now;
                boundaryWorkIds.Add(w.Id);
            }
            else
            {
                w.AnalyseToken = null;
                if (cloneToSource.TryGetValue(w.ShiftId, out var sourceShiftId))
                {
                    w.ShiftId = sourceShiftId;
                }
                promotedWorkIds.Add(w.Id);
            }
        }

        if (promotedWorkIds.Count > 0)
        {
            var promotedWorkChanges = await _context.WorkChange.IgnoreQueryFilters()
                .Where(wc => wc.AnalyseToken == token && !wc.IsDeleted && promotedWorkIds.Contains(wc.WorkId))
                .ToListAsync(ct);
            foreach (var wc in promotedWorkChanges) wc.AnalyseToken = null;

            var promotedExpenses = await _context.Expenses.IgnoreQueryFilters()
                .Where(e => e.AnalyseToken == token && !e.IsDeleted && promotedWorkIds.Contains(e.WorkId))
                .ToListAsync(ct);
            foreach (var e in promotedExpenses) e.AnalyseToken = null;
        }

        if (boundaryWorkIds.Count > 0)
        {
            var boundaryWorkChanges = await _context.WorkChange.IgnoreQueryFilters()
                .Where(wc => wc.AnalyseToken == token && !wc.IsDeleted && boundaryWorkIds.Contains(wc.WorkId))
                .ToListAsync(ct);
            foreach (var wc in boundaryWorkChanges) { wc.IsDeleted = true; wc.DeletedTime = now; }

            var boundaryExpenses = await _context.Expenses.IgnoreQueryFilters()
                .Where(e => e.AnalyseToken == token && !e.IsDeleted && boundaryWorkIds.Contains(e.WorkId))
                .ToListAsync(ct);
            foreach (var e in boundaryExpenses) { e.IsDeleted = true; e.DeletedTime = now; }
        }

        var scenarioBreaks = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted).ToListAsync(ct);
        foreach (var b in scenarioBreaks)
        {
            if (b.CurrentDate < fromDate || b.CurrentDate > untilDate)
            {
                b.IsDeleted = true;
                b.DeletedTime = now;
            }
            else
            {
                b.AnalyseToken = null;
            }
        }

        var scenarioNotes = await _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == token && !sn.IsDeleted).ToListAsync(ct);
        foreach (var sn in scenarioNotes)
        {
            if (sn.CurrentDate < fromDate || sn.CurrentDate > untilDate)
            {
                sn.IsDeleted = true;
                sn.DeletedTime = now;
            }
            else
            {
                sn.AnalyseToken = null;
            }
        }

        var scenarioShifts = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted).ToListAsync(ct);
        foreach (var s in scenarioShifts) { s.IsDeleted = true; s.DeletedTime = now; }

        var clonePreferences = await _context.Set<ClientShiftPreference>().IgnoreQueryFilters()
            .Where(p => p.AnalyseToken == token && !p.IsDeleted).ToListAsync(ct);
        foreach (var p in clonePreferences) { p.IsDeleted = true; p.DeletedTime = now; }

        var cloneShiftExpenses = await _context.Set<ShiftExpenses>().IgnoreQueryFilters()
            .Where(e => e.AnalyseToken == token && !e.IsDeleted).ToListAsync(ct);
        foreach (var e in cloneShiftExpenses) { e.IsDeleted = true; e.DeletedTime = now; }
    }

    private async Task<Dictionary<Guid, Guid>> CloneShifts(List<Guid>? groupIds, IReadOnlyCollection<Guid>? additionalShiftIds, Guid token, CancellationToken ct)
    {
        List<Shift> shifts;
        var idMap = new Dictionary<Guid, Guid>();

        if (additionalShiftIds is { Count: > 0 })
        {
            var distinctIds = additionalShiftIds.Distinct().ToList();
            shifts = await _context.Shift.IgnoreQueryFilters()
                .Where(s => !s.IsDeleted && distinctIds.Contains(s.Id))
                .Include(s => s.GroupItems.Where(gi => !gi.IsDeleted))
                .AsNoTracking()
                .ToListAsync(ct);
        }
        else
        {
            IQueryable<Shift> baseShiftQuery = _context.Shift
                .Where(s => !s.IsDeleted && s.AnalyseToken == null);

            if (groupIds != null)
            {
                var groupShiftIds = await _context.Set<GroupItem>()
                    .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ShiftId != null)
                    .Select(gi => gi.ShiftId!.Value)
                    .Distinct()
                    .ToListAsync(ct);

                baseShiftQuery = baseShiftQuery.Where(s => groupShiftIds.Contains(s.Id));
            }

            shifts = await baseShiftQuery
                .Include(s => s.GroupItems.Where(gi => !gi.IsDeleted))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        foreach (var shift in shifts)
        {
            idMap[shift.Id] = Guid.NewGuid();
        }

        var sourceIds = shifts.Select(s => s.Id).ToList();
        var childCountBySource = sourceIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.Shift.IgnoreQueryFilters()
                .Where(s => !s.IsDeleted
                    && s.AnalyseToken == null
                    && s.ParentId.HasValue
                    && sourceIds.Contains(s.ParentId.Value))
                .GroupBy(s => s.ParentId!.Value)
                .Select(g => new { ParentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ParentId, x => x.Count, ct);

        foreach (var shift in shifts)
        {
            var clone = new Shift
            {
                Id = idMap[shift.Id],
                AnalyseToken = token,
                ScenarioSourceShiftId = shift.Id,
                SourceChildCountSnapshot = childCountBySource.GetValueOrDefault(shift.Id, 0),
                CuttingAfterMidnight = shift.CuttingAfterMidnight,
                Abbreviation = shift.Abbreviation,
                Description = shift.Description,
                MacroId = shift.MacroId,
                Name = shift.Name,
                Status = shift.Status,
                AfterShift = shift.AfterShift,
                BeforeShift = shift.BeforeShift,
                EndShift = shift.EndShift,
                FromDate = shift.FromDate,
                StartShift = shift.StartShift,
                UntilDate = shift.UntilDate,
                BriefingTime = shift.BriefingTime,
                DebriefingTime = shift.DebriefingTime,
                TravelTimeAfter = shift.TravelTimeAfter,
                TravelTimeBefore = shift.TravelTimeBefore,
                IsFriday = shift.IsFriday,
                IsHoliday = shift.IsHoliday,
                IsMonday = shift.IsMonday,
                IsSaturday = shift.IsSaturday,
                IsSunday = shift.IsSunday,
                IsThursday = shift.IsThursday,
                IsTuesday = shift.IsTuesday,
                IsWednesday = shift.IsWednesday,
                IsWeekdayAndHoliday = shift.IsWeekdayAndHoliday,
                IsSporadic = shift.IsSporadic,
                SporadicScope = shift.SporadicScope,
                IsTimeRange = shift.IsTimeRange,
                Quantity = shift.Quantity,
                SumEmployees = shift.SumEmployees,
                WorkTime = shift.WorkTime,
                ShiftType = shift.ShiftType,
                OriginalId = shift.OriginalId.HasValue && idMap.ContainsKey(shift.OriginalId.Value) ? idMap[shift.OriginalId.Value] : null,
                ParentId = shift.ParentId.HasValue && idMap.ContainsKey(shift.ParentId.Value) ? idMap[shift.ParentId.Value] : null,
                RootId = shift.RootId.HasValue && idMap.ContainsKey(shift.RootId.Value) ? idMap[shift.RootId.Value] : null,
                Lft = shift.Lft,
                Rgt = shift.Rgt,
                ClientId = shift.ClientId,
                GroupItems = shift.GroupItems.Select(gi => new GroupItem
                {
                    Id = Guid.NewGuid(),
                    GroupId = gi.GroupId,
                    ClientId = gi.ClientId,
                    ShiftId = idMap[shift.Id]
                }).ToList()
            };

            await _context.Shift.AddAsync(clone, ct);
        }

        return idMap;
    }

    private async Task<Dictionary<Guid, Guid>> CloneWorks(List<Guid>? groupIds, DateOnly fromDate, DateOnly untilDate, Guid token, Dictionary<Guid, Guid> shiftIdMap, CancellationToken ct)
    {
        IQueryable<Work> topLevelQuery = _context.Work
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.ParentWorkId == null
                && w.CurrentDate >= fromDate
                && w.CurrentDate <= untilDate);

        if (groupIds != null)
        {
            var shiftIds = await _context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ShiftId != null)
                .Select(gi => gi.ShiftId!.Value)
                .Distinct()
                .ToListAsync(ct);

            topLevelQuery = topLevelQuery.Where(w => shiftIds.Contains(w.ShiftId));
        }

        var topLevelWorks = await topLevelQuery.AsNoTracking().ToListAsync(ct);
        var topLevelIds = topLevelWorks.Select(w => w.Id).ToList();

        var subWorks = topLevelIds.Count == 0
            ? new List<Work>()
            : await _context.Work
                .Where(w => !w.IsDeleted
                    && w.AnalyseToken == null
                    && w.ParentWorkId.HasValue
                    && topLevelIds.Contains(w.ParentWorkId.Value))
                .AsNoTracking()
                .ToListAsync(ct);

        var works = topLevelWorks.Concat(subWorks).ToList();

        var workIdMap = new Dictionary<Guid, Guid>();

        foreach (var work in works)
        {
            workIdMap[work.Id] = Guid.NewGuid();
        }

        foreach (var work in works)
        {
            var clone = new Work
            {
                Id = workIdMap[work.Id],
                AnalyseToken = token,
                ClientId = work.ClientId,
                CurrentDate = work.CurrentDate,
                Information = work.Information,
                WorkTime = work.WorkTime,
                Surcharges = work.Surcharges,
                StartTime = work.StartTime,
                EndTime = work.EndTime,
                LockLevel = work.LockLevel,
                SealedAt = work.SealedAt,
                SealedBy = work.SealedBy,
                ShiftId = shiftIdMap.TryGetValue(work.ShiftId, out var newShiftId) ? newShiftId : work.ShiftId,
                ParentWorkId = work.ParentWorkId.HasValue && workIdMap.TryGetValue(work.ParentWorkId.Value, out var newParentId)
                    ? newParentId
                    : null,
                TransportMode = work.TransportMode
            };

            await _context.Work.AddAsync(clone, ct);
        }

        await CloneWorkChanges(workIdMap, token, ct);
        await CloneExpenses(workIdMap, token, ct);

        return workIdMap;
    }

    private async Task CloneWorkChanges(Dictionary<Guid, Guid> workIdMap, Guid token, CancellationToken ct)
    {
        if (workIdMap.Count == 0) return;

        var originalWorkIds = workIdMap.Keys.ToList();
        var workChanges = await _context.WorkChange
            .Where(wc => !wc.IsDeleted && wc.AnalyseToken == null && originalWorkIds.Contains(wc.WorkId))
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var wc in workChanges)
        {
            var clone = new WorkChange
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                WorkId = workIdMap[wc.WorkId],
                StartTime = wc.StartTime,
                EndTime = wc.EndTime,
                ChangeTime = wc.ChangeTime,
                Surcharges = wc.Surcharges,
                Type = wc.Type,
                Description = wc.Description,
                ToInvoice = wc.ToInvoice,
                ReplaceClientId = wc.ReplaceClientId
            };

            await _context.WorkChange.AddAsync(clone, ct);
        }
    }

    private async Task CloneExpenses(Dictionary<Guid, Guid> workIdMap, Guid token, CancellationToken ct)
    {
        if (workIdMap.Count == 0) return;

        var originalWorkIds = workIdMap.Keys.ToList();
        var expenses = await _context.Expenses
            .Where(e => !e.IsDeleted && e.AnalyseToken == null && originalWorkIds.Contains(e.WorkId))
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var exp in expenses)
        {
            var clone = new Expenses
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                WorkId = workIdMap[exp.WorkId],
                Description = exp.Description,
                Amount = exp.Amount,
                Taxable = exp.Taxable
            };

            await _context.Expenses.AddAsync(clone, ct);
        }
    }

    private async Task CloneBreaks(List<Guid>? groupIds, DateOnly fromDate, DateOnly untilDate, Guid token, Dictionary<Guid, Guid> workIdMap, CancellationToken ct)
    {
        IQueryable<Break> breakQuery = _context.Break
            .Where(b => !b.IsDeleted
                && b.AnalyseToken == null
                && b.CurrentDate >= fromDate
                && b.CurrentDate <= untilDate);

        if (groupIds != null)
        {
            var clientIds = await _context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ClientId != null)
                .Select(gi => gi.ClientId!.Value)
                .Distinct()
                .ToListAsync(ct);

            var parentWorkIds = workIdMap.Keys.ToList();
            breakQuery = breakQuery.Where(b =>
                clientIds.Contains(b.ClientId)
                || (b.ParentWorkId.HasValue && parentWorkIds.Contains(b.ParentWorkId.Value)));
        }

        var breaks = await breakQuery.AsNoTracking().ToListAsync(ct);

        foreach (var brk in breaks)
        {
            var clone = new Break
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                ClientId = brk.ClientId,
                CurrentDate = brk.CurrentDate,
                Information = brk.Information,
                WorkTime = brk.WorkTime,
                Surcharges = brk.Surcharges,
                StartTime = brk.StartTime,
                EndTime = brk.EndTime,
                LockLevel = brk.LockLevel,
                SealedAt = brk.SealedAt,
                SealedBy = brk.SealedBy,
                AbsenceId = brk.AbsenceId,
                Description = brk.Description,
                ParentWorkId = brk.ParentWorkId.HasValue && workIdMap.TryGetValue(brk.ParentWorkId.Value, out var newParentId)
                    ? newParentId
                    : null
            };

            await _context.Break.AddAsync(clone, ct);
        }
    }

    private async Task CloneScheduleNotes(List<Guid>? groupIds, DateOnly fromDate, DateOnly untilDate, Guid token, CancellationToken ct)
    {
        IQueryable<ScheduleNote> noteQuery = _context.ScheduleNotes
            .Where(sn => !sn.IsDeleted
                && sn.AnalyseToken == null
                && sn.CurrentDate >= fromDate
                && sn.CurrentDate <= untilDate);

        if (groupIds != null)
        {
            var clientIds = await _context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ClientId != null)
                .Select(gi => gi.ClientId!.Value)
                .Distinct()
                .ToListAsync(ct);

            noteQuery = noteQuery.Where(sn => clientIds.Contains(sn.ClientId));
        }

        var notes = await noteQuery.AsNoTracking().ToListAsync(ct);

        foreach (var note in notes)
        {
            var clone = new ScheduleNote
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                ClientId = note.ClientId,
                CurrentDate = note.CurrentDate,
                Content = note.Content
            };

            await _context.ScheduleNotes.AddAsync(clone, ct);
        }
    }

    private async Task CloneClientShiftPreferences(Dictionary<Guid, Guid> shiftIdMap, Guid token, CancellationToken ct)
    {
        if (shiftIdMap.Count == 0) return;

        var sourceShiftIds = shiftIdMap.Keys.ToList();
        var preferences = await _context.Set<ClientShiftPreference>()
            .Where(p => !p.IsDeleted
                && p.AnalyseToken == null
                && sourceShiftIds.Contains(p.ShiftId))
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var preference in preferences)
        {
            var clone = new ClientShiftPreference
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                ClientId = preference.ClientId,
                ShiftId = shiftIdMap[preference.ShiftId],
                PreferenceType = preference.PreferenceType
            };

            await _context.Set<ClientShiftPreference>().AddAsync(clone, ct);
        }
    }

    private async Task CloneShiftExpenses(Dictionary<Guid, Guid> shiftIdMap, Guid token, CancellationToken ct)
    {
        if (shiftIdMap.Count == 0) return;

        var sourceShiftIds = shiftIdMap.Keys.ToList();
        var expenses = await _context.Set<ShiftExpenses>()
            .Where(e => !e.IsDeleted
                && e.AnalyseToken == null
                && sourceShiftIds.Contains(e.ShiftId))
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var expense in expenses)
        {
            var clone = new ShiftExpenses
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                ShiftId = shiftIdMap[expense.ShiftId],
                Amount = expense.Amount,
                Description = expense.Description,
                Taxable = expense.Taxable
            };

            await _context.Set<ShiftExpenses>().AddAsync(clone, ct);
        }
    }

    private async Task SoftDeleteRealWorks(List<Guid>? shiftIds, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        IQueryable<Work> workQuery = _context.Work
            .Where(w => !w.IsDeleted && w.AnalyseToken == null
                && w.CurrentDate >= fromDate && w.CurrentDate <= untilDate);

        if (shiftIds != null)
        {
            workQuery = workQuery.Where(w => shiftIds.Contains(w.ShiftId));
        }

        var realWorks = await workQuery.ToListAsync(ct);
        var realWorkIds = realWorks.Select(w => w.Id).ToList();

        var workChanges = await _context.WorkChange
            .Where(wc => !wc.IsDeleted && realWorkIds.Contains(wc.WorkId))
            .ToListAsync(ct);

        var expenses = await _context.Expenses
            .Where(e => !e.IsDeleted && realWorkIds.Contains(e.WorkId))
            .ToListAsync(ct);

        foreach (var wc in workChanges) { wc.IsDeleted = true; wc.DeletedTime = DateTime.UtcNow; }
        foreach (var exp in expenses) { exp.IsDeleted = true; exp.DeletedTime = DateTime.UtcNow; }
        foreach (var work in realWorks) { work.IsDeleted = true; work.DeletedTime = DateTime.UtcNow; }
    }

    private async Task SoftDeleteRealBreaks(List<Guid>? clientIds, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        IQueryable<Break> breakQuery = _context.Break
            .Where(b => !b.IsDeleted && b.AnalyseToken == null
                && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate);

        if (clientIds != null)
        {
            breakQuery = breakQuery.Where(b => clientIds.Contains(b.ClientId));
        }

        var realBreaks = await breakQuery.ToListAsync(ct);

        foreach (var brk in realBreaks) { brk.IsDeleted = true; brk.DeletedTime = DateTime.UtcNow; }
    }

    private async Task SoftDeleteRealScheduleNotes(List<Guid>? clientIds, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        IQueryable<ScheduleNote> noteQuery = _context.ScheduleNotes
            .Where(sn => !sn.IsDeleted && sn.AnalyseToken == null
                && sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate);

        if (clientIds != null)
        {
            noteQuery = noteQuery.Where(sn => clientIds.Contains(sn.ClientId));
        }

        var realNotes = await noteQuery.ToListAsync(ct);

        foreach (var note in realNotes) { note.IsDeleted = true; note.DeletedTime = DateTime.UtcNow; }
    }
}
