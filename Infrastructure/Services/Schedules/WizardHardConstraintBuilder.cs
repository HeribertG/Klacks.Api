// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Models;
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
        var preferences = await BuildShiftPreferencesAsync(agentIdList, ct);
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
        List<Guid> agentIds, CancellationToken ct)
    {
        var rawPreferences = await _context.ClientShiftPreference
            .AsNoTracking()
            .Where(p => agentIds.Contains(p.ClientId))
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
                b.Absence?.Name?.De ?? "Break"))
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
                ShiftTypeIndex: 0,
                TotalHours: w.WorkTime,
                StartAt: w.CurrentDate.ToDateTime(w.StartTime),
                EndAt: w.CurrentDate.ToDateTime(w.EndTime),
                ShiftRefId: w.ShiftId,
                LocationContext: null))
            .ToList();
    }
}
