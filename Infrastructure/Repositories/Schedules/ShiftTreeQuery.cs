// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the order families of a set of shifts as tree rows for <see cref="Klacks.Api.Domain.Services.Schedules.ShiftScopeExpander"/>:
/// the shifts themselves, their order (OriginalId ?? Id), every shift of that order, and every piece of their cut trees
/// (RootId). Scenario clones need no token filter: a clone's OriginalId, ParentId and RootId point at clones of the same
/// scenario (or are null), so a family never mixes real and scenario rows. Soft-deleted shifts stay hidden by the global filter.
/// </summary>
/// <param name="context">EF Core context holding the shifts</param>
/// <param name="shiftIds">Shifts whose families are needed</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class ShiftTreeQuery
{
    public static async Task<IReadOnlyList<ShiftTreeRow>> LoadFamiliesAsync(
        DataBaseContext context,
        IReadOnlyCollection<Guid> shiftIds,
        CancellationToken cancellationToken)
    {
        if (shiftIds.Count == 0)
        {
            return [];
        }

        var ids = shiftIds.Distinct().ToList();
        var seeds = await context.Shift
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .Select(s => new { s.Id, s.OriginalId, s.RootId })
            .ToListAsync(cancellationToken);
        if (seeds.Count == 0)
        {
            return [];
        }

        var orderIds = seeds.Select(s => s.OriginalId ?? s.Id).Distinct().ToList();
        var rootIds = seeds.Select(s => s.RootId ?? s.Id).Distinct().ToList();

        return await context.Shift
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id)
                        || orderIds.Contains(s.Id)
                        || rootIds.Contains(s.Id)
                        || (s.OriginalId != null && orderIds.Contains(s.OriginalId.Value))
                        || (s.RootId != null && rootIds.Contains(s.RootId.Value)))
            .Select(s => new ShiftTreeRow(s.Id, s.Status, s.OriginalId, s.ParentId, s.RootId))
            .ToListAsync(cancellationToken);
    }
}