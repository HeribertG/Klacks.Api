// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core-backed implementation of AnalyseScenario data operations.
/// Owns the DataBaseContext-level bulk operations that command handlers
/// used to perform inline; extracted so Create / Accept / Reject / Delete
/// handlers share one source of truth for cloning, promoting, and the
/// soft-delete + orphan sweep.
/// </summary>

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
    }

    public async Task CloneScenarioDataAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, Guid token, CancellationToken ct)
    {
        var groupIds = groupId.HasValue
            ? await GetGroupHierarchyIdsAsync(groupId.Value, ct)
            : null;

        var shiftIdMap = await CloneShifts(groupIds, token, ct);
        var workIdMap = await CloneWorks(groupIds, fromDate, untilDate, token, shiftIdMap, ct);
        await CloneBreaks(groupIds, fromDate, untilDate, token, workIdMap, ct);
        await CloneScheduleNotes(groupIds, fromDate, untilDate, token, ct);
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

    public async Task PromoteScenarioDataAsync(Guid token, CancellationToken ct)
    {
        var scenarioWorks = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && !w.IsDeleted).ToListAsync(ct);
        foreach (var w in scenarioWorks) w.AnalyseToken = null;

        var scenarioBreaks = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted).ToListAsync(ct);
        foreach (var b in scenarioBreaks) b.AnalyseToken = null;

        var scenarioShifts = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted).ToListAsync(ct);
        foreach (var s in scenarioShifts) s.AnalyseToken = null;

        var scenarioNotes = await _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == token && !sn.IsDeleted).ToListAsync(ct);
        foreach (var sn in scenarioNotes) sn.AnalyseToken = null;
    }

    private async Task<Dictionary<Guid, Guid>> CloneShifts(List<Guid>? groupIds, Guid token, CancellationToken ct)
    {
        IQueryable<Shift> shiftQuery = _context.Shift
            .Where(s => !s.IsDeleted && s.AnalyseToken == null);

        if (groupIds != null)
        {
            var shiftIds = await _context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ShiftId != null)
                .Select(gi => gi.ShiftId!.Value)
                .Distinct()
                .ToListAsync(ct);

            shiftQuery = shiftQuery.Where(s => shiftIds.Contains(s.Id));
        }

        var shifts = await shiftQuery
            .Include(s => s.GroupItems.Where(gi => !gi.IsDeleted))
            .AsNoTracking()
            .ToListAsync(ct);

        var idMap = new Dictionary<Guid, Guid>();

        foreach (var shift in shifts)
        {
            idMap[shift.Id] = Guid.NewGuid();
        }

        foreach (var shift in shifts)
        {
            var clone = new Shift
            {
                Id = idMap[shift.Id],
                AnalyseToken = token,
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

        await CloneWorkChanges(workIdMap, ct);
        await CloneExpenses(workIdMap, ct);

        return workIdMap;
    }

    private async Task CloneWorkChanges(Dictionary<Guid, Guid> workIdMap, CancellationToken ct)
    {
        if (workIdMap.Count == 0) return;

        var originalWorkIds = workIdMap.Keys.ToList();
        var workChanges = await _context.WorkChange
            .Where(wc => !wc.IsDeleted && originalWorkIds.Contains(wc.WorkId))
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var wc in workChanges)
        {
            var clone = new WorkChange
            {
                Id = Guid.NewGuid(),
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

    private async Task CloneExpenses(Dictionary<Guid, Guid> workIdMap, CancellationToken ct)
    {
        if (workIdMap.Count == 0) return;

        var originalWorkIds = workIdMap.Keys.ToList();
        var expenses = await _context.Expenses
            .Where(e => !e.IsDeleted && originalWorkIds.Contains(e.WorkId))
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var exp in expenses)
        {
            var clone = new Expenses
            {
                Id = Guid.NewGuid(),
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
