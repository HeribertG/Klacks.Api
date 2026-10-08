// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the shift preferences (Preferred / Blacklist) of the planning agents for one scenario (AnalyseToken, null =
/// real plan) and hands them down the order tree with <see cref="ShiftScopeExpander"/>, so a preference set on an order
/// or a cut piece also reaches the cut pieces the wizards actually staff. Single source of Wizard 1 and the harmonizer.
/// </summary>
/// <param name="context">EF Core context holding the preferences and shifts</param>
/// <param name="agentIds">Planning agents</param>
/// <param name="analyseToken">Scenario token; null = real plan</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class ShiftPreferenceScopeQuery
{
    public static async Task<IReadOnlyList<ScopedShiftPreference>> LoadAsync(
        DataBaseContext context,
        IReadOnlyCollection<Guid> agentIds,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var agentIdList = agentIds.ToList();
        var stored = await context.ClientShiftPreference
            .AsNoTracking()
            .Where(p => agentIdList.Contains(p.ClientId)
                        && (p.AnalyseToken == analyseToken || (p.AnalyseToken == null && analyseToken == null)))
            .Select(p => new ScopedShiftPreference(p.ClientId, p.ShiftId, p.PreferenceType))
            .ToListAsync(cancellationToken);
        if (stored.Count == 0)
        {
            return stored;
        }

        var rows = await ShiftTreeQuery.LoadFamiliesAsync(
            context, stored.Select(p => p.ShiftId).Distinct().ToList(), cancellationToken);
        return ShiftScopeExpander.ExpandPreferences(stored, rows);
    }
}