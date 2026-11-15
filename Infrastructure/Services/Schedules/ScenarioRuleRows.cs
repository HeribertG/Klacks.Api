// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The rows a rule check reads. The real plan reads its own rows (AnalyseToken null). A scenario reads its own rows plus
/// the rows of the real plan that stay in the plan after an accept and that the scenario does not carry, so a rule counts
/// what the plan will hold: a real work on a shift no shift of the scenario stands for (ScenarioSourceShiftId), a real work
/// on a carried shift beyond the span the scenario cloned (its dates widened by ScenarioConstants.BoundaryDays), the real
/// absences the scenario holds no twin of (same person, day and absence type, as ScenarioRealAbsenceGuard matches them),
/// the pauses of such real works, and the changes of the visible works. A real work on a carried shift inside the cloned
/// span is the scenario's business and counts through its clone; rows of another scenario never count. The rule checks tell
/// a clone from its source by shift and span, the cover-move facts by Work.ClonedFromWorkId (CoverMoveFactsReader). A work
/// of the scenario on the same shift, person and day as a real work is its twin and stands for it, like an absence twin;
/// this covers the clones that keep the real shift (container children on a task shift the scenario does not clone) and
/// works placed in the scenario on a real shift. A token without a live scenario row has no span, so only the real works
/// on shifts it does not carry and without a twin count.
/// </summary>
/// <param name="context">Database context of the caller; the queries leave soft-deleted rows out themselves</param>
/// <param name="analyseToken">Scenario token of the check; null reads the real plan</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class ScenarioRuleRows
{
    public static IQueryable<Work> Works(DataBaseContext context, Guid? analyseToken)
        => analyseToken is not { } token
            ? context.Work.Where(w => w.AnalyseToken == null && !w.IsDeleted)
            : VisibleWorks(context, token);

    public static IQueryable<Break> Breaks(DataBaseContext context, Guid? analyseToken)
    {
        if (analyseToken is not { } token)
        {
            return context.Break.Where(b => b.AnalyseToken == null && !b.IsDeleted);
        }

        var visibleWorks = VisibleWorks(context, token);
        var scenarioAbsences = context.Break.Where(s => s.AnalyseToken == token && s.ParentWorkId == null && !s.IsDeleted);

        return context.Break.Where(b => !b.IsDeleted
            && (b.AnalyseToken == token
            || (b.AnalyseToken == null
                && ((b.ParentWorkId == null
                        && !scenarioAbsences.Any(s => s.ClientId == b.ClientId
                            && s.CurrentDate == b.CurrentDate
                            && s.AbsenceId == b.AbsenceId))
                    || (b.ParentWorkId != null && visibleWorks.Any(w => w.Id == b.ParentWorkId))))));
    }

    public static IQueryable<WorkChange> WorkChanges(DataBaseContext context, Guid? analyseToken)
    {
        if (analyseToken is not { } token)
        {
            return context.WorkChange.Where(wc => wc.Work!.AnalyseToken == null && !wc.IsDeleted);
        }

        var visibleWorks = VisibleWorks(context, token);

        return context.WorkChange.Where(wc => !wc.IsDeleted && visibleWorks.Any(w => w.Id == wc.WorkId));
    }

    private static IQueryable<Work> VisibleWorks(DataBaseContext context, Guid token)
    {
        var carriedShiftIds = OutsideScenarioWorkQuery.CarriedShiftIds(context, token);
        var scenarios = context.AnalyseScenarios.Where(s => s.Token == token && !s.IsDeleted);
        var scenarioWorks = context.Work.Where(s => s.AnalyseToken == token && !s.IsDeleted);

        return context.Work.Where(w => !w.IsDeleted
            && (w.AnalyseToken == token
                || (w.AnalyseToken == null
                    && !scenarioWorks.Any(s => s.ShiftId == w.ShiftId
                        && s.ClientId == w.ClientId
                        && s.CurrentDate == w.CurrentDate)
                    && (!carriedShiftIds.Contains(w.ShiftId)
                        || scenarios.Any(s => w.CurrentDate < s.FromDate.AddDays(-ScenarioConstants.BoundaryDays)
                            || w.CurrentDate > s.UntilDate.AddDays(ScenarioConstants.BoundaryDays))))));
    }
}
