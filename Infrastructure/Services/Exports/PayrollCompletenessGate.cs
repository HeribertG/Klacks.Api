// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Completeness gate of the person-based payroll export. A person is in scope when they have a non-deleted,
/// non-scenario Work or Break of any lock level in the period. Three blocker kinds: (1) EntryNotClosed - an entry
/// below LockLevel.Closed (per person and day); (2) DayNotLocked - a day with an entry or an active group membership
/// that no period-close seal (SealedDay.Level Closed) locks for the person, resolved by DayLockAttribution so a
/// global seal, a group seal with membership and a group seal with a worked shift mean the same as in the day lock;
/// a day-approval row (Level Approved) never counts; (3) OverlappingExport - an export of the person for another
/// period that overlaps this one. All data is loaded in a fixed number of set-based queries.
/// </summary>
/// <param name="context">EF Core context holding Work, Break, Client, GroupItem, Group, Membership and SealedDay</param>
/// <param name="exportLogItems">Per-person export records, for the overlapping-period check</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class PayrollCompletenessGate : IPayrollCompletenessGate
{
    private readonly DataBaseContext _context;
    private readonly IExportLogItemRepository _exportLogItems;

    public PayrollCompletenessGate(DataBaseContext context, IExportLogItemRepository exportLogItems)
    {
        _context = context;
        _exportLogItems = exportLogItems;
    }

    public async Task<PayrollCompletenessResult> CheckAsync(
        DateOnly from,
        DateOnly until,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken = default)
    {
        var entries = await LoadEntriesAsync(from, until, clientIds, cancellationToken);
        var personIds = entries.Select(e => e.ClientId).Distinct().ToList();
        if (personIds.Count == 0)
        {
            return new PayrollCompletenessResult();
        }

        var persons = await LoadPersonsAsync(personIds, cancellationToken);
        var shiftGroups = await LoadShiftGroupsAsync(entries, cancellationToken);
        var memberGroupIds = await LoadMemberGroupIdsAsync(personIds, cancellationToken);
        var groupNames = await LoadGroupNamesAsync(
            shiftGroups.Values.SelectMany(g => g).Concat(memberGroupIds).ToHashSet(), cancellationToken);

        var blockers = new List<PayrollExportBlockerDto>();
        AddEntryNotClosedBlockers(blockers, entries, persons, shiftGroups, groupNames);
        await AddDayNotLockedBlockersAsync(
            blockers, entries, persons, shiftGroups, memberGroupIds, groupNames, from, until, cancellationToken);
        await AddOverlappingExportBlockersAsync(blockers, personIds, persons, from, until, cancellationToken);

        var ordered = blockers
            .OrderBy(b => b.ClientName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(b => b.ClientId)
            .ThenBy(b => b.Date)
            .ThenBy(b => b.Reason)
            .ToList();

        return new PayrollCompletenessResult
        {
            Blockers = ordered.Take(PayrollExportConstants.MaxReportedBlockers).ToList(),
            BlockerTotal = ordered.Count,
        };
    }

    private async Task<List<DayEntry>> LoadEntriesAsync(
        DateOnly from,
        DateOnly until,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken)
    {
        var restricted = clientIds?.ToList();

        var worksQuery = _context.Work
            .AsNoTracking()
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.CurrentDate >= from
                && w.CurrentDate <= until
                && w.Client != null
                && (w.Client.Type == EntityTypeEnum.Employee || w.Client.Type == EntityTypeEnum.ExternEmp));

        var breaksQuery = _context.Break
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                && b.AnalyseToken == null
                && b.CurrentDate >= from
                && b.CurrentDate <= until
                && b.Client != null
                && (b.Client.Type == EntityTypeEnum.Employee || b.Client.Type == EntityTypeEnum.ExternEmp));

        if (restricted is not null)
        {
            worksQuery = worksQuery.Where(w => restricted.Contains(w.ClientId));
            breaksQuery = breaksQuery.Where(b => restricted.Contains(b.ClientId));
        }

        var works = await worksQuery
            .Select(w => new { w.ClientId, w.CurrentDate, w.LockLevel, w.ShiftId })
            .ToListAsync(cancellationToken);
        var breaks = await breaksQuery
            .Select(b => new { b.ClientId, b.CurrentDate, b.LockLevel })
            .ToListAsync(cancellationToken);

        return works
            .Select(w => new DayEntry(w.ClientId, w.CurrentDate, w.LockLevel, w.ShiftId))
            .Concat(breaks.Select(b => new DayEntry(b.ClientId, b.CurrentDate, b.LockLevel, null)))
            .ToList();
    }

    private async Task<Dictionary<Guid, PersonInfo>> LoadPersonsAsync(
        List<Guid> personIds,
        CancellationToken cancellationToken)
    {
        var rows = await _context.Client
            .AsNoTracking()
            .Where(c => personIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.FirstName, c.IdNumber })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            c => c.Id,
            c => new PersonInfo(ClientNameFormatter.LastFirst(c.Name, c.FirstName), c.IdNumber));
    }

    private async Task<Dictionary<Guid, List<Guid>>> LoadShiftGroupsAsync(
        List<DayEntry> entries,
        CancellationToken cancellationToken)
    {
        var shiftIds = entries
            .Where(e => e.ShiftId.HasValue)
            .Select(e => e.ShiftId!.Value)
            .Distinct()
            .ToList();
        if (shiftIds.Count == 0)
        {
            return [];
        }

        var rows = await _context.GroupItem
            .AsNoTracking()
            .Where(gi => !gi.IsDeleted
                && gi.AnalyseToken == null
                && gi.ScenarioSourceGroupItemId == null
                && gi.ShiftId != null
                && shiftIds.Contains(gi.ShiftId.Value))
            .Select(gi => new { ShiftId = gi.ShiftId!.Value, gi.GroupId })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.ShiftId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.GroupId).ToList());
    }

    private async Task<HashSet<Guid>> LoadMemberGroupIdsAsync(
        List<Guid> personIds,
        CancellationToken cancellationToken)
    {
        var groupIds = await _context.GroupItem
            .AsNoTracking()
            .Where(gi => !gi.IsDeleted
                && gi.AnalyseToken == null
                && gi.ScenarioSourceGroupItemId == null
                && gi.ClientId != null
                && personIds.Contains(gi.ClientId.Value))
            .Select(gi => gi.GroupId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return groupIds.ToHashSet();
    }

    private async Task<Dictionary<Guid, string>> LoadGroupNamesAsync(
        HashSet<Guid> groupIds,
        CancellationToken cancellationToken)
    {
        if (groupIds.Count == 0)
        {
            return [];
        }

        var ids = groupIds.ToList();
        var rows = await _context.Group
            .AsNoTracking()
            .Where(g => !g.IsDeleted && ids.Contains(g.Id))
            .Select(g => new { g.Id, g.Name })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(g => g.Id, g => g.Name);
    }

    private static void AddEntryNotClosedBlockers(
        List<PayrollExportBlockerDto> blockers,
        List<DayEntry> entries,
        Dictionary<Guid, PersonInfo> persons,
        Dictionary<Guid, List<Guid>> shiftGroups,
        Dictionary<Guid, string> groupNames)
    {
        var openDays = entries
            .Where(e => e.LockLevel != WorkLockLevel.Closed)
            .GroupBy(e => (e.ClientId, e.Date));

        foreach (var day in openDays)
        {
            var groupId = FirstGroupByName(
                day.Where(e => e.ShiftId.HasValue)
                    .SelectMany(e => shiftGroups.GetValueOrDefault(e.ShiftId!.Value) ?? []),
                groupNames);

            blockers.Add(CreateBlocker(
                day.Key.ClientId,
                persons,
                day.Key.Date,
                PayrollExportBlockReason.EntryNotClosed,
                groupId,
                groupNames,
                requiresGlobalClose: false,
                day.Count()));
        }
    }

    private async Task AddDayNotLockedBlockersAsync(
        List<PayrollExportBlockerDto> blockers,
        List<DayEntry> entries,
        Dictionary<Guid, PersonInfo> persons,
        Dictionary<Guid, List<Guid>> shiftGroups,
        HashSet<Guid> memberGroupIds,
        Dictionary<Guid, string> groupNames,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken)
    {
        var existingMemberGroupIds = memberGroupIds.Where(groupNames.ContainsKey).ToList();
        var windows = await GroupMembershipWindowLoader.LoadAsync(
            _context, existingMemberGroupIds, persons.Keys.ToList(), from, until, cancellationToken);

        var entriesByDay = entries
            .GroupBy(e => (e.ClientId, e.Date))
            .ToDictionary(g => g.Key, g => g.ToList());

        var requiredDays = new Dictionary<(Guid ClientId, DateOnly Date), List<Guid>>();
        foreach (var key in entriesByDay.Keys)
        {
            requiredDays[key] = [];
        }

        foreach (var (groupId, window) in windows)
        {
            var firstDay = MaxDay(from, window.MembershipFrom, window.GroupItemFrom);
            var lastDay = MinDay(until, window.MembershipUntil, window.GroupItemUntil);

            for (var dayNumber = firstDay.DayNumber; dayNumber <= lastDay.DayNumber; dayNumber++)
            {
                var day = DateOnly.FromDayNumber(dayNumber);
                if (!requiredDays.TryGetValue((window.ClientId, day), out var groups))
                {
                    groups = [];
                    requiredDays[(window.ClientId, day)] = groups;
                }

                groups.Add(groupId);
            }
        }

        var pairs = requiredDays.Keys.Select(k => (k.Date, k.ClientId)).ToList();
        var locking = await DayLockAttribution.LoadLockingSealsAsync(
            _context, pairs, null, cancellationToken, WorkLockLevel.Closed);

        foreach (var ((clientId, date), memberGroups) in requiredDays)
        {
            if (locking.ContainsKey((date, clientId)))
            {
                continue;
            }

            var entriesOfDay = entriesByDay.GetValueOrDefault((clientId, date)) ?? [];
            var candidates = memberGroups.Concat(entriesOfDay
                .Where(e => e.ShiftId.HasValue)
                .SelectMany(e => shiftGroups.GetValueOrDefault(e.ShiftId!.Value) ?? []));
            var groupId = FirstGroupByName(candidates, groupNames);

            blockers.Add(CreateBlocker(
                clientId,
                persons,
                date,
                PayrollExportBlockReason.DayNotLocked,
                groupId,
                groupNames,
                requiresGlobalClose: groupId is null,
                entriesOfDay.Count));
        }
    }

    private async Task AddOverlappingExportBlockersAsync(
        List<PayrollExportBlockerDto> blockers,
        List<Guid> personIds,
        Dictionary<Guid, PersonInfo> persons,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken)
    {
        var overlapping = await _exportLogItems.GetOverlappingAsync(personIds, from, until, cancellationToken);

        foreach (var clientId in overlapping.Select(i => i.ClientId).Distinct())
        {
            blockers.Add(CreateBlocker(
                clientId,
                persons,
                null,
                PayrollExportBlockReason.OverlappingExport,
                null,
                [],
                requiresGlobalClose: false,
                0));
        }
    }

    private static DateOnly MaxDay(DateOnly bound, DateOnly first, DateOnly? second)
    {
        var latest = first > bound ? first : bound;
        return second.HasValue && second.Value > latest ? second.Value : latest;
    }

    private static DateOnly MinDay(DateOnly bound, DateOnly? first, DateOnly? second)
    {
        var earliest = bound;
        if (first.HasValue && first.Value < earliest)
        {
            earliest = first.Value;
        }

        return second.HasValue && second.Value < earliest ? second.Value : earliest;
    }

    private static Guid? FirstGroupByName(IEnumerable<Guid> candidates, Dictionary<Guid, string> groupNames)
    {
        return candidates
            .Distinct()
            .Where(groupNames.ContainsKey)
            .OrderBy(id => groupNames[id], StringComparer.OrdinalIgnoreCase)
            .ThenBy(id => id)
            .Select(id => (Guid?)id)
            .FirstOrDefault();
    }

    private static PayrollExportBlockerDto CreateBlocker(
        Guid clientId,
        Dictionary<Guid, PersonInfo> persons,
        DateOnly? date,
        PayrollExportBlockReason reason,
        Guid? groupId,
        Dictionary<Guid, string> groupNames,
        bool requiresGlobalClose,
        int entryCount)
    {
        var person = persons.GetValueOrDefault(clientId);

        return new PayrollExportBlockerDto
        {
            ClientId = clientId,
            ClientName = person?.Name ?? string.Empty,
            IdNumber = person?.IdNumber ?? 0,
            Date = date,
            GroupId = groupId,
            GroupName = groupId.HasValue ? groupNames.GetValueOrDefault(groupId.Value) : null,
            Reason = reason,
            RequiresGlobalClose = requiresGlobalClose,
            EntryCount = entryCount,
        };
    }

    private sealed record DayEntry(Guid ClientId, DateOnly Date, WorkLockLevel LockLevel, Guid? ShiftId);

    private sealed record PersonInfo(string Name, int IdNumber);
}
