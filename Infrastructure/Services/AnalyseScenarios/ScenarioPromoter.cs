// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Promote step of an accept: moves the scenario's works of the period (with their work changes and expenses) onto the
/// real shifts, discards boundary works and the works the day-lock plan keeps out, stamps the applied replacement
/// requests, promotes breaks and notes, discards the scenario shifts and their shift-scoped rows, turns temporary
/// memberships into real ones and drops the scenario's planning constraints. Everything is staged on the shared context;
/// the accept handler commits. Returns and logs what the day lock kept out.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="dayLockPlanBuilder">Builds the day-lock plan of the accept</param>
/// <param name="replacementRequestBook">Stamps the replacement requests whose change is promoted</param>
/// <param name="lockSkipCounter">Counts the discarded rows the real plan does not keep</param>
/// <param name="dayRowPromoter">Promotes the scenario breaks and schedule notes</param>
/// <param name="ownedRowStager">Drops the scenario's planning constraints</param>
/// <param name="logger">Logs what the day lock kept out of the accept</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioPromoter
{
    private const string LockSkipsMessage =
        "Accepting scenario {Token}: {Works} works, {WorkChanges} work changes, {Expenses} expenses, {Breaks} breaks and {Notes} schedule notes on locked days were not taken over";

    private readonly DataBaseContext _context;
    private readonly AcceptDayLockPlanBuilder _dayLockPlanBuilder;
    private readonly ScenarioReplacementRequestBook _replacementRequestBook;
    private readonly ScenarioLockSkipCounter _lockSkipCounter;
    private readonly ScenarioDayRowPromoter _dayRowPromoter;
    private readonly ScenarioOwnedRowStager _ownedRowStager;
    private readonly ILogger<ScenarioPromoter> _logger;

    public ScenarioPromoter(
        DataBaseContext context,
        AcceptDayLockPlanBuilder dayLockPlanBuilder,
        ScenarioReplacementRequestBook replacementRequestBook,
        ScenarioLockSkipCounter lockSkipCounter,
        ScenarioDayRowPromoter dayRowPromoter,
        ScenarioOwnedRowStager ownedRowStager,
        ILogger<ScenarioPromoter> logger)
    {
        _context = context;
        _dayLockPlanBuilder = dayLockPlanBuilder;
        _replacementRequestBook = replacementRequestBook;
        _lockSkipCounter = lockSkipCounter;
        _dayRowPromoter = dayRowPromoter;
        _ownedRowStager = ownedRowStager;
        _logger = logger;
    }

    public async Task<ScenarioAcceptLockSkips> PromoteScenarioWorksAsync(Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        var plan = await _dayLockPlanBuilder.BuildAcceptDayLockPlanAsync(token, fromDate, untilDate, [], [], ct);

        var cloneShifts = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted && s.ScenarioSourceShiftId.HasValue)
            .Select(s => new { s.Id, SourceId = s.ScenarioSourceShiftId!.Value })
            .ToListAsync(ct);

        var cloneToSource = cloneShifts.ToDictionary(c => c.Id, c => c.SourceId);
        var now = DateTime.UtcNow;

        var scenarioWorks = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && !w.IsDeleted).ToListAsync(ct);
        var (promotedWorkIds, boundaryWorkIds, discardedInPeriod, skippedWorks) =
            ApplyToScenarioWorks(scenarioWorks, fromDate, untilDate, plan, cloneToSource, now);

        await _replacementRequestBook.StageReplacementRequestsAppliedAsync(token, promotedWorkIds, cloneToSource, now, ct);
        await PromoteWorkChildrenAsync(token, promotedWorkIds, ct);
        await DiscardWorkChildrenAsync(token, boundaryWorkIds, now, ct);

        var (skippedWorkChanges, skippedExpenses) = await _lockSkipCounter.CountDiscardedChildChangesAsync(discardedInPeriod, plan, ct);
        var skippedBreaks = await _dayRowPromoter.PromoteScenarioBreaksAsync(token, fromDate, untilDate, plan, now, ct);
        var skippedNotes = await _dayRowPromoter.PromoteScenarioNotesAsync(token, fromDate, untilDate, plan, now, ct);

        await DiscardScenarioShiftRowsAsync(token, now, ct);
        await PromoteScenarioMembershipsAsync(token, now, ct);

        await _ownedRowStager.SoftDeleteScenarioPlanningConstraintsAsync(token, now, ct);

        var skips = new ScenarioAcceptLockSkips(
            skippedWorks, skippedBreaks, skippedNotes, skippedWorkChanges, skippedExpenses);
        LogLockSkips(token, skips);
        return skips;
    }

    /// <summary>
    /// Promotes the scenario works of the period onto their real shifts, and discards the boundary works and the works
    /// whose slot the day-lock plan keeps (counting those that differ from their real origin).
    /// </summary>
    private static (List<Guid> Promoted, List<Guid> Boundary, List<Guid> DiscardedInPeriod, int SkippedWorks) ApplyToScenarioWorks(
        List<Work> scenarioWorks, DateOnly fromDate, DateOnly untilDate, AcceptDayLockPlan plan,
        IReadOnlyDictionary<Guid, Guid> cloneToSource, DateTime now)
    {
        var promotedWorkIds = new List<Guid>();
        var boundaryWorkIds = new List<Guid>();
        var skippedWorks = 0;
        var discardedInPeriod = new List<Guid>();
        foreach (var w in scenarioWorks)
        {
            if (w.CurrentDate < fromDate || w.CurrentDate > untilDate)
            {
                w.IsDeleted = true;
                w.DeletedTime = now;
                boundaryWorkIds.Add(w.Id);
            }
            else if (plan.DiscardsClone(w.Id))
            {
                // Locked slot (locked clone or origin, or a locked day on either side): the delete keeps the real
                // work, so promoting this clone would duplicate it or apply half a move. Discard the clone instead.
                w.IsDeleted = true;
                w.DeletedTime = now;
                boundaryWorkIds.Add(w.Id);
                discardedInPeriod.Add(w.Id);
                if (ScenarioLockSkipCounter.DiffersFromOrigin(w, plan.RealOriginOf(w.Id), cloneToSource))
                {
                    skippedWorks++;
                }
            }
            else
            {
                w.AnalyseToken = null;
                w.ClonedFromWorkId = null;
                if (cloneToSource.TryGetValue(w.ShiftId, out var sourceShiftId))
                {
                    w.ShiftId = sourceShiftId;
                }
                promotedWorkIds.Add(w.Id);
            }
        }

        return (promotedWorkIds, boundaryWorkIds, discardedInPeriod, skippedWorks);
    }

    private async Task PromoteWorkChildrenAsync(Guid token, List<Guid> promotedWorkIds, CancellationToken ct)
    {
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
    }

    private async Task DiscardWorkChildrenAsync(Guid token, List<Guid> boundaryWorkIds, DateTime now, CancellationToken ct)
    {
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
    }

    /// <summary>
    /// Discards the scenario shifts (their works are promoted onto the real shifts) with their required qualifications,
    /// and the scenario's schedule commands, client shift preferences and shift expenses.
    /// </summary>
    private async Task DiscardScenarioShiftRowsAsync(Guid token, DateTime now, CancellationToken ct)
    {
        var scenarioShifts = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted).ToListAsync(ct);
        foreach (var s in scenarioShifts) { s.IsDeleted = true; s.DeletedTime = now; }

        var cloneShiftIds = scenarioShifts.Select(s => s.Id).ToList();
        if (cloneShiftIds.Count > 0)
        {
            var requiredQualifications = await _context.ShiftRequiredQualification.IgnoreQueryFilters()
                .Where(srq => !srq.IsDeleted && cloneShiftIds.Contains(srq.ShiftId)).ToListAsync(ct);
            foreach (var srq in requiredQualifications) { srq.IsDeleted = true; srq.DeletedTime = now; }
        }

        var cloneCommands = await _context.ScheduleCommands.IgnoreQueryFilters()
            .Where(sc => sc.AnalyseToken == token && !sc.IsDeleted).ToListAsync(ct);
        foreach (var sc in cloneCommands) { sc.IsDeleted = true; sc.DeletedTime = now; }

        var clonePreferences = await _context.Set<ClientShiftPreference>().IgnoreQueryFilters()
            .Where(p => p.AnalyseToken == token && !p.IsDeleted).ToListAsync(ct);
        foreach (var p in clonePreferences) { p.IsDeleted = true; p.DeletedTime = now; }

        var cloneShiftExpenses = await _context.Set<ShiftExpenses>().IgnoreQueryFilters()
            .Where(e => e.AnalyseToken == token && !e.IsDeleted).ToListAsync(ct);
        foreach (var e in cloneShiftExpenses) { e.IsDeleted = true; e.DeletedTime = now; }
    }

    /// <summary>
    /// Promotes the temporary cross-group client memberships to real (time-bounded) ones, and drops the cloned-shift
    /// memberships along with their (discarded) cloned shifts.
    /// </summary>
    private async Task PromoteScenarioMembershipsAsync(Guid token, DateTime now, CancellationToken ct)
    {
        var scenarioMemberships = await _context.Set<GroupItem>().IgnoreQueryFilters()
            .Where(gi => gi.AnalyseToken == token && !gi.IsDeleted).ToListAsync(ct);
        foreach (var gi in scenarioMemberships)
        {
            if (gi.ShiftId != null)
            {
                gi.IsDeleted = true;
                gi.DeletedTime = now;
            }
            else
            {
                gi.AnalyseToken = null;
            }
        }
    }

    private void LogLockSkips(Guid token, ScenarioAcceptLockSkips skips)
    {
        if (skips.Total > 0)
        {
            _logger.LogInformation(
                LockSkipsMessage, token, skips.Works, skips.WorkChanges, skips.Expenses, skips.Breaks, skips.Notes);
        }
    }
}
