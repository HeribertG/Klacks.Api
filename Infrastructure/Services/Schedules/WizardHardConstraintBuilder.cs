// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Models;
using Klacks.ScheduleOptimizer.TokenEvolution.Initialization;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="IWizardHardConstraintBuilder"/> that reads directly from the DbContext.
/// All four constraint sets are filtered by agent ids, period and the scenario AnalyseToken
/// (using IS NOT DISTINCT FROM semantics to propagate null as "main scenario").
/// </summary>
/// <param name="context">EF Core database context</param>
public sealed class WizardHardConstraintBuilder : IWizardHardConstraintBuilder
{
    private readonly DataBaseContext _context;

    public WizardHardConstraintBuilder(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<HardConstraintResult> BuildAsync(
        IReadOnlyList<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct)
    {
        var agentIdList = agentIds.ToList();

        var commands = await BuildScheduleCommandsAsync(agentIdList, from, until, analyseToken, ct);
        var preferences = await BuildShiftPreferencesAsync(agentIdList, analyseToken, ct);
        var blockers = await BuildBreakBlockersAsync(agentIdList, from, until, analyseToken, ct);
        var lockedWorks = await BuildLockedWorksAsync(agentIdList, from, until, analyseToken, ct);
        var existingBlockers = await BuildExistingWorkBlockersAsync(agentIdList, from, until, analyseToken, ct);

        return new HardConstraintResult(commands, preferences, blockers, lockedWorks, existingBlockers);
    }

    private async Task<IReadOnlyList<CoreScheduleCommand>> BuildScheduleCommandsAsync(
        List<Guid> agentIds, DateOnly from, DateOnly until, Guid? analyseToken, CancellationToken ct)
    {
        var rawCommands = await _context.ScheduleCommands
            .AsNoTracking()
            .Where(c => agentIds.Contains(c.ClientId)
                        && c.CurrentDate >= from
                        && c.CurrentDate <= until
                        && (c.AnalyseToken == analyseToken || (c.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);

        var result = new List<CoreScheduleCommand>(rawCommands.Count);
        foreach (var cmd in rawCommands)
        {
            if (ScheduleCommandKeywordMapper.TryMap(cmd.CommandKeyword, out var keyword))
            {
                result.Add(new CoreScheduleCommand(cmd.ClientId.ToString(), cmd.CurrentDate, keyword));
            }
        }

        return result;
    }

    private async Task<IReadOnlyList<CoreShiftPreference>> BuildShiftPreferencesAsync(
        List<Guid> agentIds, Guid? analyseToken, CancellationToken ct)
    {
        var rawPreferences = await _context.ClientShiftPreference
            .AsNoTracking()
            .Where(p => agentIds.Contains(p.ClientId)
                        && (p.AnalyseToken == analyseToken || (p.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);

        return rawPreferences
            .Select(p => new CoreShiftPreference(
                p.ClientId.ToString(),
                p.ShiftId,
                p.PreferenceType == ShiftPreferenceType.Preferred
                    ? ShiftPreferenceKind.Preferred
                    : ShiftPreferenceKind.Blacklist))
            .ToList();
    }

    private async Task<IReadOnlyList<CoreBreakBlocker>> BuildBreakBlockersAsync(
        List<Guid> agentIds, DateOnly from, DateOnly until, Guid? analyseToken, CancellationToken ct)
    {
        var rawBreaks = await _context.Break
            .AsNoTracking()
            .Include(b => b.Absence)
            .Where(b => agentIds.Contains(b.ClientId)
                        && b.CurrentDate >= from
                        && b.CurrentDate <= until
                        && (b.AnalyseToken == analyseToken || (b.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);

        return rawBreaks
            .Select(b => new CoreBreakBlocker(
                b.ClientId.ToString(),
                b.CurrentDate,
                b.CurrentDate,
                b.Absence?.Name?.De ?? "Break",
                b.WorkTime))
            .ToList();
    }

    private async Task<IReadOnlyList<CoreExistingWorkBlocker>> BuildExistingWorkBlockersAsync(
        List<Guid> agentIds, DateOnly from, DateOnly until, Guid? analyseToken, CancellationToken ct)
    {
        var rawWorks = await _context.Work
            .AsNoTracking()
            .Where(w => agentIds.Contains(w.ClientId)
                        && w.CurrentDate >= from
                        && w.CurrentDate <= until
                        && w.LockLevel == WorkLockLevel.None
                        && (w.AnalyseToken == analyseToken || (w.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);

        // Scenario mode: cloned works only cover the cloned (group-scoped) shifts. An in-scope agent
        // may also hold an unlocked Work on an out-of-group shift that was never cloned — invisible to
        // the overlap / min-pause checks, which would let the wizard double-book that agent against a
        // real DB work (and after Accept produce two overlapping REAL works). Load those real
        // (token=null) works VETO-only and de-dup by (agent, date, start, end) against the clones.
        // CloneWorks is intentionally NOT widened to the client axis — that would corrupt the promote
        // round-trip; these entries are read-only blockers the engine never plans, scores or mutates.
        if (analyseToken != null)
        {
            var realWorks = await _context.Work
                .AsNoTracking()
                .Where(w => agentIds.Contains(w.ClientId)
                            && w.CurrentDate >= from
                            && w.CurrentDate <= until
                            && w.LockLevel == WorkLockLevel.None
                            && w.AnalyseToken == null)
                .ToListAsync(ct);

            var clonedKeys = rawWorks
                .Select(w => (w.ClientId, w.CurrentDate, w.StartTime, w.EndTime))
                .ToHashSet();

            rawWorks.AddRange(realWorks.Where(w =>
                !clonedKeys.Contains((w.ClientId, w.CurrentDate, w.StartTime, w.EndTime))));
        }

        return rawWorks
            .Select(w =>
            {
                var startAt = w.CurrentDate.ToDateTime(w.StartTime);
                var endAt = w.EndTime <= w.StartTime
                    ? w.CurrentDate.AddDays(1).ToDateTime(w.EndTime)
                    : w.CurrentDate.ToDateTime(w.EndTime);
                return new CoreExistingWorkBlocker(
                    AgentId: w.ClientId.ToString(),
                    Date: w.CurrentDate,
                    StartAt: startAt,
                    EndAt: endAt);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<CoreLockedWork>> BuildLockedWorksAsync(
        List<Guid> agentIds, DateOnly from, DateOnly until, Guid? analyseToken, CancellationToken ct)
    {
        var rawWorks = await _context.Work
            .AsNoTracking()
            .Where(w => agentIds.Contains(w.ClientId)
                        && w.CurrentDate >= from
                        && w.CurrentDate <= until
                        && w.LockLevel > WorkLockLevel.None
                        && (w.AnalyseToken == analyseToken || (w.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);

        return rawWorks
            .Select(w => new CoreLockedWork(
                WorkId: w.Id.ToString(),
                AgentId: w.ClientId.ToString(),
                Date: w.CurrentDate,
                ShiftTypeIndex: ShiftTypeInference.FromStartTime(w.StartTime),
                TotalHours: w.WorkTime,
                StartAt: w.CurrentDate.ToDateTime(w.StartTime),
                EndAt: w.CurrentDate.ToDateTime(w.EndTime),
                ShiftRefId: w.ShiftId,
                LocationContext: null)
            {
                Surcharges = w.Surcharges,
            })
            .ToList();
    }
}
