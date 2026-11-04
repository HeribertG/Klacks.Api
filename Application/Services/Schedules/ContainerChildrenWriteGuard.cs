// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IContainerChildrenWriteGuard"/>. It gives the children of a container save what the update handlers
/// of Work and Break give a row on its own: the seal state is carried over from the stored row (an entity rebuilt from a
/// request has none and would unseal the row), the scenario token and the clone link come from the stored row, the lock
/// level of the stored rows and of the container is enforced by IParentWorkLockGuard and BreakLockRule, and the day locks
/// are checked for the stored and the new day and employee. A child that is left out is deleted by the save, so it passes
/// the checks of a delete: its lock level (a sealed sub-break needs an admin, BreakLockRule.EnsureDeletable), its sealed
/// day and, in a scenario, the delete check of sealed days. New children
/// may bring ids of their own, the container dialog creates them; an id a row has taken already is answered like a missing
/// container. The owner of every sub-work is server-owned as well (ContainerChildOwner): the customer of its shift, else the
/// person of the container, whatever client the request names (an empty one included).
/// </summary>
/// <param name="readRepository">Tells which ids are taken already</param>
/// <param name="lockGuard">Applies the lock level rule of a Work</param>
/// <param name="dayLockService">Refuses writes on sealed days</param>
/// <param name="httpContextAccessor">Source of the calling user's roles</param>
/// <param name="shiftRepository">Reads the shifts of all sub-works at once, for their customer and to place the work changes on their days</param>

using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class ContainerChildrenWriteGuard : IContainerChildrenWriteGuard
{
    private readonly IContainerWorkChildrenReadRepository _readRepository;
    private readonly IParentWorkLockGuard _lockGuard;
    private readonly IDayLockService _dayLockService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IShiftRepository _shiftRepository;

    public ContainerChildrenWriteGuard(
        IContainerWorkChildrenReadRepository readRepository,
        IParentWorkLockGuard lockGuard,
        IDayLockService dayLockService,
        IHttpContextAccessor httpContextAccessor,
        IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository;
        _readRepository = readRepository;
        _lockGuard = lockGuard;
        _dayLockService = dayLockService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task PrepareAsync(
        Work container,
        StoredContainerChildren stored,
        IReadOnlyList<Work> works,
        IReadOnlyList<Break> breaks,
        IReadOnlyList<WorkChange> workChanges,
        CancellationToken cancellationToken)
    {
        var storedWorks = stored.Works.ToDictionary(work => work.Id);
        var storedBreaks = stored.Breaks.ToDictionary(entry => entry.Id);
        var storedChanges = stored.WorkChanges.ToDictionary(change => change.Id);

        await EnsureNewIdsAreFreeAsync(container, works, breaks, workChanges, storedWorks, storedBreaks, storedChanges, cancellationToken);
        var shifts = await LoadShiftsAsync(stored, works, cancellationToken);
        TakeServerOwnedState(container, works, breaks, workChanges, storedWorks, storedBreaks, shifts);

        EnsureLockLevelsAllowWriting(container, stored, breaks);
        await EnsureDaysAreOpenAsync(container, stored, works, breaks, workChanges, storedChanges, shifts, cancellationToken);
    }

    private async Task EnsureNewIdsAreFreeAsync(
        Work container,
        IReadOnlyList<Work> works,
        IReadOnlyList<Break> breaks,
        IReadOnlyList<WorkChange> workChanges,
        IReadOnlyDictionary<Guid, Work> storedWorks,
        IReadOnlyDictionary<Guid, Break> storedBreaks,
        IReadOnlyDictionary<Guid, WorkChange> storedChanges,
        CancellationToken cancellationToken)
    {
        var newWorkIds = works.Select(work => work.Id).Where(id => id != Guid.Empty && !storedWorks.ContainsKey(id)).Distinct().ToList();
        var newBreakIds = breaks.Select(entry => entry.Id).Where(id => id != Guid.Empty && !storedBreaks.ContainsKey(id)).Distinct().ToList();
        var newChangeIds = workChanges.Select(change => change.Id).Where(id => id != Guid.Empty && !storedChanges.ContainsKey(id)).Distinct().ToList();
        if (newWorkIds.Count == 0 && newBreakIds.Count == 0 && newChangeIds.Count == 0)
        {
            return;
        }

        var used = await _readRepository.GetUsedEntryIds(newWorkIds, newBreakIds, newChangeIds, cancellationToken);
        if (used.Works.Count > 0 || used.Breaks.Count > 0 || used.WorkChanges.Count > 0)
        {
            throw new KeyNotFoundException(string.Format(ContainerLockMessages.WorkNotFound, container.Id));
        }
    }

    private async Task<IReadOnlyDictionary<Guid, Shift>> LoadShiftsAsync(
        StoredContainerChildren stored, IReadOnlyList<Work> works, CancellationToken cancellationToken)
    {
        var shiftIds = works.Select(work => work.ShiftId).Concat(stored.Works.Select(work => work.ShiftId)).Distinct().ToList();
        if (shiftIds.Count == 0)
        {
            return new Dictionary<Guid, Shift>();
        }

        return (await _shiftRepository.GetWithClientByIdsAsync(shiftIds, cancellationToken)).ToDictionary(shift => shift.Id);
    }

    private static void TakeServerOwnedState(
        Work container,
        IReadOnlyList<Work> works,
        IReadOnlyList<Break> breaks,
        IReadOnlyList<WorkChange> workChanges,
        IReadOnlyDictionary<Guid, Work> storedWorks,
        IReadOnlyDictionary<Guid, Break> storedBreaks,
        IReadOnlyDictionary<Guid, Shift> shifts)
    {
        foreach (var work in works)
        {
            storedWorks.TryGetValue(work.Id, out var storedWork);
            work.ClientId = ContainerChildOwner.Of(shifts.GetValueOrDefault(work.ShiftId)?.ClientId, container.ClientId);
            ScheduleEntrySealState.CarryOver(work, storedWork);
            work.AnalyseToken = storedWork != null ? storedWork.AnalyseToken : container.AnalyseToken;
            work.ClonedFromWorkId = storedWork?.ClonedFromWorkId;
            work.ParentWorkId = container.Id;
        }

        foreach (var entry in breaks)
        {
            storedBreaks.TryGetValue(entry.Id, out var storedBreak);
            ScheduleEntrySealState.CarryOver(entry, storedBreak);
            entry.AnalyseToken = storedBreak != null ? storedBreak.AnalyseToken : container.AnalyseToken;
            entry.ParentWorkId = container.Id;
            ContainerBreakDefaults.Apply(entry, container);
        }

        foreach (var change in workChanges)
        {
            change.AnalyseToken = container.AnalyseToken;
        }
    }

    private void EnsureLockLevelsAllowWriting(Work container, StoredContainerChildren stored, IReadOnlyList<Break> breaks)
    {
        _lockGuard.EnsureWorkWritableForCaller(container, _httpContextAccessor);
        _lockGuard.EnsureChildrenWritableForCaller(stored.Works, stored.Breaks, _httpContextAccessor);

        var keptBreakIds = breaks.Select(entry => entry.Id).ToHashSet();
        var removedBreaks = stored.Breaks.Where(entry => !keptBreakIds.Contains(entry.Id)).ToList();
        _lockGuard.EnsureChildrenDeletableForCaller(new ContainerChildren([], removedBreaks), _httpContextAccessor);
    }

    private async Task EnsureDaysAreOpenAsync(
        Work container,
        StoredContainerChildren stored,
        IReadOnlyList<Work> works,
        IReadOnlyList<Break> breaks,
        IReadOnlyList<WorkChange> workChanges,
        IReadOnlyDictionary<Guid, WorkChange> storedChanges,
        IReadOnlyDictionary<Guid, Shift> shifts,
        CancellationToken cancellationToken)
    {
        var days = new HashSet<(DateOnly Date, Guid ClientId, Guid? AnalyseToken)>
        {
            (container.CurrentDate, container.ClientId, container.AnalyseToken),
        };
        days.UnionWith(stored.Works.Select(work => (work.CurrentDate, work.ClientId, work.AnalyseToken)));
        days.UnionWith(stored.Breaks.Select(entry => (entry.CurrentDate, entry.ClientId, entry.AnalyseToken)));
        days.UnionWith(works.Select(work => (work.CurrentDate, work.ClientId, work.AnalyseToken)));
        days.UnionWith(breaks.Select(entry => (entry.CurrentDate, entry.ClientId, entry.AnalyseToken)));
        days.UnionWith(WorkChangeDays(stored, works, workChanges, storedChanges, shifts));
        await _dayLockService.EnsureNoneLockedAsync(days.ToList(), cancellationToken);

        var keptIds = works.Select(work => work.Id).ToHashSet();
        var deletedInScenario = stored.Works
            .Where(work => work.AnalyseToken.HasValue && !keptIds.Contains(work.Id))
            .Select(work => (work.CurrentDate, work.ClientId))
            .Distinct()
            .ToList();
        if (deletedInScenario.Count > 0)
        {
            await _dayLockService.EnsureDeletableAsync(deletedInScenario, cancellationToken);
        }
    }

    /// <summary>
    /// The days the schedule shows the written or removed work changes on, for the employee of the sub-work and for the
    /// replacement employee, as the single-change check of the day lock service resolves them: from the shift times read
    /// once for all sub-works, with WorkChangeEntryDay. The sub-work is the stored one when there is one, so a request
    /// cannot name another employee for a stored sub-work; scenario sub-works are not day-locked. A new or changed change
    /// and one that is left out count, an unchanged one does not.
    /// </summary>
    private static IReadOnlyList<(DateOnly Date, Guid ClientId, Guid? AnalyseToken)> WorkChangeDays(
        StoredContainerChildren stored,
        IReadOnlyList<Work> works,
        IReadOnlyList<WorkChange> workChanges,
        IReadOnlyDictionary<Guid, WorkChange> storedChanges,
        IReadOnlyDictionary<Guid, Shift> shifts)
    {
        var parents = stored.Works.ToDictionary(work => work.Id);
        foreach (var work in works.Where(work => work.Id != Guid.Empty))
        {
            parents.TryAdd(work.Id, work);
        }

        var keptChangeIds = workChanges.Select(change => change.Id).ToHashSet();
        var written = workChanges.Where(change =>
            !storedChanges.TryGetValue(change.Id, out var storedChange) || !WorkChangeContent.AreSame(storedChange, change));
        var checkedChanges = stored.WorkChanges.Where(change => !keptChangeIds.Contains(change.Id)).Concat(written)
            .Where(change => parents.TryGetValue(change.WorkId, out var parent) && !parent.AnalyseToken.HasValue)
            .ToList();
        if (checkedChanges.Count == 0)
        {
            return [];
        }

        var days = new List<(DateOnly Date, Guid ClientId, Guid? AnalyseToken)>();
        foreach (var change in checkedChanges)
        {
            var parent = parents[change.WorkId];
            var day = shifts.TryGetValue(parent.ShiftId, out var shift)
                ? WorkChangeEntryDay.Of(
                    change.Type, parent.CurrentDate, parent.EndTime, change.StartTime, change.ChangeTime, shift.StartShift, shift.EndShift)
                : parent.CurrentDate;
            days.Add((day, parent.ClientId, null));
            if (change.ReplaceClientId is { } replaceClientId)
            {
                days.Add((day, replaceClientId, null));
            }
        }

        return days;
    }
}
