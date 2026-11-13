// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Clones the employee-day rows of a scenario run: breaks (absences and work pauses), schedule notes and schedule
/// commands of the window, limited to the employees of the group. Pauses follow their cloned parent work. The employee
/// scope is handed in by the coordinator, which reads it once through the reader the accept uses for its scope.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="clientScope">Employees of the group hierarchy (regular members, and with a source scenario its borrowed employees); null without a group</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioDayRowCloner
{
    private readonly DataBaseContext _context;

    public ScenarioDayRowCloner(DataBaseContext context)
    {
        _context = context;
    }

    internal async Task CloneBreaks((List<Guid> Real, List<Guid> WithSource)? clientScope, DateOnly fromDate, DateOnly untilDate, Guid token, Dictionary<Guid, Guid> workIdMap, ScenarioCloneSource source, CancellationToken ct)
    {
        var breaks = source.Token is null
            ? await LoadRealPlanBreaksAsync(clientScope, fromDate, untilDate, workIdMap, ct)
            : await LoadSourcedBreaksAsync(clientScope, fromDate, untilDate, workIdMap, source, ct);

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

    internal async Task CloneScheduleNotes((List<Guid> Real, List<Guid> WithSource)? clientScope, DateOnly fromDate, DateOnly untilDate, Guid token, ScenarioCloneSource source, CancellationToken ct)
    {
        IQueryable<ScheduleNote> noteQuery = ScenarioSourceFilters.WhereFromSource(_context.ScheduleNotes
            .Where(sn => !sn.IsDeleted
                && sn.CurrentDate >= fromDate
                && sn.CurrentDate <= untilDate), source);

        if (clientScope is { } noteScope)
        {
            noteQuery = ScenarioSourceFilters.WhereClientInScope(noteQuery, noteScope, source);
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

    internal async Task CloneScheduleCommands((List<Guid> Real, List<Guid> WithSource)? clientScope, DateOnly fromDate, DateOnly untilDate, Guid token, ScenarioCloneSource source, CancellationToken ct)
    {
        IQueryable<ScheduleCommand> commandQuery = ScenarioSourceFilters.WhereFromSource(_context.ScheduleCommands
            .Where(sc => !sc.IsDeleted
                && sc.CurrentDate >= fromDate
                && sc.CurrentDate <= untilDate), source);

        if (clientScope is { } commandScope)
        {
            commandQuery = ScenarioSourceFilters.WhereClientInScope(commandQuery, commandScope, source);
        }

        var commands = await commandQuery.AsNoTracking().ToListAsync(ct);

        foreach (var command in commands)
        {
            var clone = new ScheduleCommand
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                ClientId = command.ClientId,
                CurrentDate = command.CurrentDate,
                CommandKeyword = command.CommandKeyword
            };

            await _context.ScheduleCommands.AddAsync(clone, ct);
        }
    }

    private async Task<List<Break>> LoadRealPlanBreaksAsync((List<Guid> Real, List<Guid> WithSource)? clientScope, DateOnly fromDate, DateOnly untilDate, Dictionary<Guid, Guid> workIdMap, CancellationToken ct)
    {
        IQueryable<Break> breakQuery = _context.Break
            .Where(b => !b.IsDeleted
                && b.AnalyseToken == null
                && b.CurrentDate >= fromDate
                && b.CurrentDate <= untilDate);

        if (clientScope is { } scope)
        {
            var clientIds = scope.Real;

            var parentWorkIds = workIdMap.Keys.ToList();
            breakQuery = breakQuery.Where(b =>
                clientIds.Contains(b.ClientId)
                || (b.ParentWorkId.HasValue && parentWorkIds.Contains(b.ParentWorkId.Value)));
        }

        return await breakQuery.AsNoTracking().ToListAsync(ct);
    }

    /// <summary>
    /// Breaks of a scenario-sourced clone. Standalone breaks (absences) follow the date split of the source range and the
    /// employee scope; sub-breaks (work pauses) follow their cloned parent work, exactly like sub-works, so a night work at
    /// the source range's edge keeps the pause that falls on the next day.
    /// </summary>
    private async Task<List<Break>> LoadSourcedBreaksAsync(
        (List<Guid> Real, List<Guid> WithSource)? clientScope, DateOnly fromDate, DateOnly untilDate, Dictionary<Guid, Guid> workIdMap,
        ScenarioCloneSource source, CancellationToken ct)
    {
        var standaloneQuery = ScenarioSourceFilters.WhereFromSource(_context.Break
            .Where(b => !b.IsDeleted
                && b.ParentWorkId == null
                && b.CurrentDate >= fromDate
                && b.CurrentDate <= untilDate), source);

        if (clientScope is { } standaloneScope)
        {
            standaloneQuery = ScenarioSourceFilters.WhereClientInScope(standaloneQuery, standaloneScope, source);
        }

        var breaks = await standaloneQuery.AsNoTracking().ToListAsync(ct);

        var parentWorkIds = workIdMap.Keys.ToList();
        if (parentWorkIds.Count > 0)
        {
            breaks.AddRange(await ScenarioSourceFilters.WhereEitherWorld(_context.Break
                    .Where(b => !b.IsDeleted && b.ParentWorkId.HasValue && parentWorkIds.Contains(b.ParentWorkId.Value)), source)
                .AsNoTracking()
                .ToListAsync(ct));
        }

        return breaks;
    }
}
