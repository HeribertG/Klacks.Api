// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Real works of the run's agents that a scenario does not carry: works of the real plan (no token) on a real shift that no
/// shift of the scenario stands for (ScenarioSourceShiftId). A group scenario clones only the group's shifts, so a work the
/// person holds in another group lives outside it, yet stays in the plan after an accept. Only works that count their own
/// hours are read (WorkOwnHours); a child of the person's own container lies inside the container's time.
/// </summary>
/// <param name="context">Database context of the caller (soft-delete filters apply to Work)</param>
/// <param name="analyseToken">Token of the scenario the run reads</param>
/// <param name="agentIds">Agents of the run</param>
/// <param name="from">First date (inclusive)</param>
/// <param name="until">Last date (inclusive)</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class OutsideScenarioWorkQuery
{
    public static async Task<List<Work>> LoadAsync(
        DataBaseContext context,
        Guid analyseToken,
        IReadOnlyCollection<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        CancellationToken ct)
        => await Query(context, analyseToken, agentIds.ToList(), from, until).ToListAsync(ct);

    internal static IQueryable<Work> Query(
        DataBaseContext context,
        Guid analyseToken,
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until)
    {
        var carriedShiftIds = context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == analyseToken && !s.IsDeleted && s.ScenarioSourceShiftId.HasValue)
            .Select(s => s.ScenarioSourceShiftId!.Value);

        return context.Work
            .AsNoTracking()
            .Where(w => agentIds.Contains(w.ClientId)
                        && w.AnalyseToken == null
                        && w.CurrentDate >= from
                        && w.CurrentDate <= until
                        && !carriedShiftIds.Contains(w.ShiftId))
            .Where(WorkOwnHours.Counts);
    }
}
