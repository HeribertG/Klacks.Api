// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one rule for what still uses a set of shifts: works of any kind that are not deleted, scenario clones and container
/// references. Shared by the delete of a whole family and the removal of parts by an end date.
/// </summary>
/// <param name="context">Database context of the calling repository</param>
/// <param name="shiftIds">Ids of the shifts to check</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class OrderFamilyUsageQuery
{
    public static async Task<OrderFamilyUsage> ReadAsync(
        DataBaseContext context, IReadOnlyList<Guid> shiftIds, CancellationToken cancellationToken)
    {
        var hasWorks = await context.Work
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(w => !w.IsDeleted && shiftIds.Contains(w.ShiftId), cancellationToken);

        var hasScenarioClones = await context.Shift
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(s => !s.IsDeleted && s.ScenarioSourceShiftId.HasValue && shiftIds.Contains(s.ScenarioSourceShiftId.Value), cancellationToken);

        var hasContainerReferences =
            await context.ContainerTemplateItem
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(i => !i.IsDeleted && i.ShiftId.HasValue && shiftIds.Contains(i.ShiftId.Value), cancellationToken)
            || await context.ContainerShiftOverrideItems
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(i => !i.IsDeleted && i.ShiftId.HasValue && shiftIds.Contains(i.ShiftId.Value), cancellationToken);

        return new OrderFamilyUsage(shiftIds, hasWorks, hasScenarioClones, hasContainerReferences);
    }
}
