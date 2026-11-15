// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The twin rule between a scenario and the real plan, read by the rule checks (ScenarioRuleRows) and by the wizard runs
/// (OutsideScenarioWorkQuery). A live work of the scenario on the same shift, person and day as a real work stands for it
/// when it is that work's clone (ClonedFromWorkId) or runs the same hours (StartTime and EndTime, which ScenarioWorkCloner
/// copies 1:1). The clone link keeps a clone whose hours were edited in the scenario a twin; the hours tell a work placed in
/// the scenario, or a clone of a source scenario, from the person's other works on that shift and day. This covers the
/// clones that keep the real shift (container children on a task shift the scenario does not clone) and works placed in
/// the scenario on a real shift.
/// </summary>
/// <param name="works">Rows to filter; rows with a token pass unchanged</param>
/// <param name="context">Database context of the caller</param>
/// <param name="token">Scenario token whose works stand for their real twins</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class ScenarioWorkTwins
{
    internal static IQueryable<Work> WithoutRealTwins(IQueryable<Work> works, DataBaseContext context, Guid token)
    {
        var scenarioWorks = context.Work.Where(s => s.AnalyseToken == token && !s.IsDeleted);

        return works.Where(w => w.AnalyseToken != null
            || !scenarioWorks.Any(s => s.ShiftId == w.ShiftId
                && s.ClientId == w.ClientId
                && s.CurrentDate == w.CurrentDate
                && (s.ClonedFromWorkId == w.Id || (s.StartTime == w.StartTime && s.EndTime == w.EndTime))));
    }
}
