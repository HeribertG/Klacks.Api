// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Wizard 1 slot replacement in a scenario clone: the planner's works replace the movable incumbent on exactly the
/// (shift, date) slots it fills (grill H2). Expenses of a displaced work are never dropped: they move to the planner's
/// work on the same slot (replacing the shift default expenses BulkAdd staged there); with several works on one slot each
/// displaced work keeps its own employee's new work first, the rest are matched one to one and only leftovers share a work.
/// A slot that received no planner work (placement blocked or without clone) is emptied; the expenses then follow the
/// employee's own new work that day, and only without one does the displaced work carrying expenses stay with its children.
/// A work on a locked day (sealed for its employee) is never displaced: a scenario must not delete it (owner 2026-10-13).
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public partial class AnalyseScenarioService
{
    public async Task<IReadOnlyList<Guid>> SoftDeleteClonedWorksOnSlotsAsync(Guid token, DateOnly fromDate, DateOnly untilDate, IReadOnlySet<(Guid ShiftId, DateOnly Date)> plannedSlots, CancellationToken ct)
    {
        // A movable work on a slot the planner did NOT plan is preserved: the wizard run can cover a narrower
        // agent/shift set than the group-scoped clone. Locked works, replacement covers, corrected works and standalone
        // absence breaks are always immutable. Expenses stay attached to the displaced work until
        // ResolveDisplacedWorksAsync moves them.
        if (plannedSlots.Count == 0)
        {
            return [];
        }

        var candidateWorks = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && w.CurrentDate >= fromDate && w.CurrentDate <= untilDate
                && !w.IsDeleted && w.LockLevel == WorkLockLevel.None && w.ParentWorkId == null)
            .ToListAsync(ct);
        var fixedWorkIds = await Schedules.CorrectedWorkQuery.LoadFixedWorkIdsAsync(_context, token, fromDate, untilDate, ct);
        var lockedDays = await Repositories.Schedules.DayLockAttribution.LoadLockedClientDaysAsync(
            _context, candidateWorks.Select(w => w.ClientId).Distinct().ToList(), fromDate, untilDate, ct);
        var movableWorks = candidateWorks
            .Where(w => plannedSlots.Contains((w.ShiftId, w.CurrentDate))
                && !fixedWorkIds.Contains(w.Id)
                && !lockedDays.Contains((w.ClientId, w.CurrentDate)))
            .ToList();
        if (movableWorks.Count == 0)
        {
            return [];
        }

        var workIds = movableWorks.Select(w => w.Id).ToList();

        // One shared deletion stamp, truncated to the database's microsecond precision, identifies the children deleted
        // together with a work when RestoreDisplacedWorkAsync undoes the replacement.
        var utcNow = DateTime.UtcNow;
        var now = utcNow.AddTicks(-(utcNow.Ticks % TimeSpan.TicksPerMicrosecond));

        var subBreaks = await _context.Break.IgnoreQueryFilters()
            .Where(b => !b.IsDeleted && b.ParentWorkId.HasValue && workIds.Contains(b.ParentWorkId.Value))
            .ToListAsync(ct);
        foreach (var b in subBreaks) { b.IsDeleted = true; b.DeletedTime = now; }

        var workChanges = await _context.WorkChange
            .Where(wc => !wc.IsDeleted && workIds.Contains(wc.WorkId))
            .ToListAsync(ct);
        foreach (var wc in workChanges) { wc.IsDeleted = true; wc.DeletedTime = now; }

        foreach (var w in movableWorks) { w.IsDeleted = true; w.DeletedTime = now; }

        return workIds;
    }

    public async Task<int> ResolveDisplacedWorksAsync(Guid token, IReadOnlyList<Guid> displacedWorkIds, IReadOnlyList<Guid> createdWorkIds, CancellationToken ct)
    {
        if (displacedWorkIds.Count == 0)
        {
            return 0;
        }

        var displacedWorks = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && w.IsDeleted && displacedWorkIds.Contains(w.Id))
            .ToListAsync(ct);
        var createdWorks = createdWorkIds.Count == 0
            ? []
            : await _context.Work.IgnoreQueryFilters()
                .Where(w => w.AnalyseToken == token && !w.IsDeleted && w.ParentWorkId == null && createdWorkIds.Contains(w.Id))
                .ToListAsync(ct);
        var createdBySlot = createdWorks
            .GroupBy(w => (w.ShiftId, w.CurrentDate))
            .ToDictionary(g => g.Key, g => g.ToList());

        var displacedIds = displacedWorks.Select(w => w.Id).ToList();
        var expensesByWork = (await _context.Expenses
                .Where(e => !e.IsDeleted && displacedIds.Contains(e.WorkId))
                .ToListAsync(ct))
            .GroupBy(e => e.WorkId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var createdIds = createdWorks.Select(w => w.Id).ToList();
        var templateExpensesByWork = (await _context.Expenses
                .Where(e => !e.IsDeleted && createdIds.Contains(e.WorkId))
                .ToListAsync(ct))
            .GroupBy(e => e.WorkId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var slotSuccessors = new Dictionary<Guid, Work>();
        var oneToOne = new HashSet<Guid>();
        foreach (var slot in displacedWorks.GroupBy(w => (w.ShiftId, w.CurrentDate)))
        {
            if (createdBySlot.TryGetValue(slot.Key, out var slotWorks))
            {
                MatchSlotSuccessors(slot.ToList(), slotWorks, slotSuccessors, oneToOne);
            }
        }

        var claimed = new HashSet<Guid>();
        var expensesChangingEmployee = 0;
        var processingOrder = displacedWorks
            .OrderBy(w => oneToOne.Contains(w.Id) ? 0 : 1)
            .ThenBy(w => w.StartTime)
            .ThenBy(w => w.Id)
            .ToList();
        foreach (var displaced in processingOrder)
        {
            if (!slotSuccessors.TryGetValue(displaced.Id, out var successor))
            {
                // Slot left empty (placement blocked): the slot is emptied. Expenses must survive, so they follow the
                // employee's own new work that day; without one, the incumbent carrying expenses stays with its children.
                successor = createdWorks
                    .Where(w => w.ClientId == displaced.ClientId && w.CurrentDate == displaced.CurrentDate)
                    .OrderBy(w => w.StartTime)
                    .ThenBy(w => w.Id)
                    .FirstOrDefault();
                if (successor is null)
                {
                    if (expensesByWork.ContainsKey(displaced.Id))
                    {
                        await RestoreDisplacedWorkAsync(displaced, ct);
                    }

                    continue;
                }
            }

            // Lineage only follows a successor on the displaced work's own slot; a work on another shift that day is a
            // different work, not a continuation of the displaced work's origin.
            if (successor.ShiftId == displaced.ShiftId && successor.CurrentDate == displaced.CurrentDate)
            {
                successor.ClonedFromWorkId ??= displaced.ClonedFromWorkId;
            }

            if (!expensesByWork.TryGetValue(displaced.Id, out var expenses))
            {
                continue;
            }

            // A work created in this apply only carries its shift's default expenses (BulkAdd stages them); the
            // displaced work's expenses already hold them (or the planner removed them deliberately), so the defaults
            // are dropped instead of doubling the payroll.
            if (claimed.Add(successor.Id) && templateExpensesByWork.TryGetValue(successor.Id, out var templates))
            {
                var now = DateTime.UtcNow;
                foreach (var template in templates) { template.IsDeleted = true; template.DeletedTime = now; }
            }

            foreach (var expense in expenses)
            {
                expense.WorkId = successor.Id;
            }

            if (successor.ClientId != displaced.ClientId)
            {
                expensesChangingEmployee += expenses.Count;
            }
        }

        return expensesChangingEmployee;
    }

    /// <summary>
    /// Assigns the displaced works of one (shift, date) slot to the planner's works of that slot (Shift.Quantity can put
    /// several of each on one slot), independent of which displaced works carry expenses and of load order: first every
    /// displaced work takes the new work of its own employee, then the remaining displaced works take the remaining new
    /// works one to one, and only displaced works left over after that are spread round-robin over the slot's works.
    /// Both lists are ordered by start time and id, so the assignment is deterministic.
    /// </summary>
    /// <param name="displaced">Displaced works of the slot</param>
    /// <param name="created">Planner's works of the slot (at least one)</param>
    /// <param name="successors">Receives displaced work id -> successor</param>
    /// <param name="oneToOne">Receives the displaced work ids that got a successor of their own (not a leftover)</param>
    private static void MatchSlotSuccessors(
        IReadOnlyList<Work> displaced, IReadOnlyList<Work> created, IDictionary<Guid, Work> successors, ISet<Guid> oneToOne)
    {
        var orderedDisplaced = displaced.OrderBy(w => w.StartTime).ThenBy(w => w.Id).ToList();
        var orderedCreated = created.OrderBy(w => w.StartTime).ThenBy(w => w.Id).ToList();
        var free = new List<Work>(orderedCreated);

        foreach (var work in orderedDisplaced)
        {
            var own = free.FirstOrDefault(c => c.ClientId == work.ClientId);
            if (own is not null)
            {
                successors[work.Id] = own;
                oneToOne.Add(work.Id);
                free.Remove(own);
            }
        }

        foreach (var work in orderedDisplaced.Where(w => !successors.ContainsKey(w.Id)))
        {
            if (free.Count == 0)
            {
                break;
            }

            successors[work.Id] = free[0];
            oneToOne.Add(work.Id);
            free.RemoveAt(0);
        }

        var next = 0;
        foreach (var work in orderedDisplaced.Where(w => !successors.ContainsKey(w.Id)))
        {
            successors[work.Id] = orderedCreated[next % orderedCreated.Count];
            next++;
        }
    }
    /// <summary>
    /// Undoes the slot replacement of one displaced work that carries expenses: the work and the sub-breaks and WorkChanges soft-deleted
    /// together with it (same deletion stamp) come back; its expenses were never deleted.
    /// </summary>
    private async Task RestoreDisplacedWorkAsync(Work displaced, CancellationToken ct)
    {
        var deletedAt = displaced.DeletedTime;

        var subBreaks = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.IsDeleted && b.ParentWorkId == displaced.Id && b.DeletedTime == deletedAt)
            .ToListAsync(ct);
        foreach (var b in subBreaks) { b.IsDeleted = false; b.DeletedTime = null; }

        var workChanges = await _context.WorkChange.IgnoreQueryFilters()
            .Where(wc => wc.IsDeleted && wc.WorkId == displaced.Id && wc.DeletedTime == deletedAt)
            .ToListAsync(ct);
        foreach (var wc in workChanges) { wc.IsDeleted = false; wc.DeletedTime = null; }

        displaced.IsDeleted = false;
        displaced.DeletedTime = null;
    }
}
