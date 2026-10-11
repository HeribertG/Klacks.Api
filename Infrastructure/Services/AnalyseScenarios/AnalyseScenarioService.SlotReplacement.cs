// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Wizard 1 slot replacement in a scenario clone: the planner's works replace the movable incumbent on exactly the
/// (shift, date) slots it fills (grill H2). Expenses of a displaced work are never dropped: they move to the planner's
/// work on the same slot (replacing the shift default expenses BulkAdd staged there). A slot that received no planner work
/// (placement blocked or without clone) is emptied; the expenses then follow the employee's own new work that day, and
/// only without one does the displaced work carrying expenses stay with its children.
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
        var movableWorks = candidateWorks
            .Where(w => plannedSlots.Contains((w.ShiftId, w.CurrentDate)) && !fixedWorkIds.Contains(w.Id))
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

        var claimed = new HashSet<Guid>();
        var expensesChangingEmployee = 0;
        foreach (var displaced in displacedWorks)
        {
            Work? successor;
            if (createdBySlot.TryGetValue((displaced.ShiftId, displaced.CurrentDate), out var slotWorks))
            {
                successor = slotWorks.FirstOrDefault(w => w.ClientId == displaced.ClientId)
                    ?? slotWorks.FirstOrDefault(w => !claimed.Contains(w.Id))
                    ?? slotWorks[0];
            }
            else
            {
                // Slot left empty (placement blocked): the slot is emptied. Expenses must survive, so they follow the
                // employee's own new work that day; without one, the incumbent carrying expenses stays with its children.
                successor = createdWorks.FirstOrDefault(w => w.ClientId == displaced.ClientId && w.CurrentDate == displaced.CurrentDate);
                if (successor is null)
                {
                    if (expensesByWork.ContainsKey(displaced.Id))
                    {
                        await RestoreDisplacedWorkAsync(displaced, ct);
                    }

                    continue;
                }
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

            successor.ClonedFromWorkId ??= displaced.ClonedFromWorkId;

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
