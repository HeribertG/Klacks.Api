// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Finds the works an accept removes: an absence the scenario added to a work that stayed uncovered. The absence must be new
/// against the real plan, so a work the planner left next to an older absence stays. On-call absences take no work, a sealed,
/// replaced, started or sub work stays (CoverMoveDecision), from every path including the cover dialog, and a scenario without
/// a group finds nothing, because only a group's clones are known to be its own.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="factsReader">Tells which scenario works are untouched</param>
/// <param name="companyClock">Tells which works have started</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioOpenSlotFinder
{
    private readonly DataBaseContext _context;
    private readonly ICoverMoveFactsReader _factsReader;
    private readonly ICompanyClock _companyClock;

    public ScenarioOpenSlotFinder(DataBaseContext context, ICoverMoveFactsReader factsReader, ICompanyClock companyClock)
    {
        _context = context;
        _factsReader = factsReader;
        _companyClock = companyClock;
    }

    internal async Task<IReadOnlySet<Guid>> FindAsync(Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        var isGroupScenario = await _context.AnalyseScenarios.AsNoTracking()
            .AnyAsync(s => s.Token == token && s.GroupId != null && s.Kind == AnalyseScenarioKind.Scenario, ct);
        if (!isGroupScenario)
        {
            return new HashSet<Guid>();
        }

        var absences = await (
                from b in _context.Break.AsNoTracking()
                join a in _context.Absence.AsNoTracking() on b.AbsenceId equals a.Id
                where b.AnalyseToken == token
                    && b.ParentWorkId == null
                    && b.CurrentDate >= fromDate
                    && b.CurrentDate <= untilDate
                    && !a.IsOnCall
                    && !_context.Break.Any(real => real.AnalyseToken == null
                        && real.ClientId == b.ClientId
                        && real.CurrentDate == b.CurrentDate
                        && real.AbsenceId == b.AbsenceId)
                select new { b.ClientId, b.CurrentDate, b.StartTime, b.EndTime })
            .ToListAsync(ct);
        if (absences.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var clientIds = absences.Select(a => a.ClientId).Distinct().ToList();
        var works = await _context.Work.AsNoTracking()
            .Where(w => w.AnalyseToken == token
                && w.ParentWorkId == null
                && w.CurrentDate >= fromDate
                && w.CurrentDate <= untilDate
                && clientIds.Contains(w.ClientId))
            .Select(w => new { w.Id, w.ClientId, w.CurrentDate, w.StartTime, w.EndTime })
            .ToListAsync(ct);

        var taken = works
            .Where(w => absences.Any(a => a.ClientId == w.ClientId
                && a.CurrentDate == w.CurrentDate
                && AbsenceWorkCoverage.CoversWork(a.StartTime, a.EndTime, w.StartTime, w.EndTime)))
            .Select(w => w.Id)
            .ToList();
        if (taken.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var facts = await _factsReader.LoadAsync(taken, token, ct);
        var nowUtc = (await _companyClock.GetNowAsync(ct)).UtcDateTime;
        var companyTimeZone = await _companyClock.GetTimeZoneAsync(ct);
        return facts.Values
            .Where(fact => CoverMoveDecision.MayFall(fact, nowUtc, companyTimeZone))
            .Select(fact => fact.WorkId)
            .ToHashSet();
    }
}
