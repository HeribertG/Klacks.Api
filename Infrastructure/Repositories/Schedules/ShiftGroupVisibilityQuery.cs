// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies the Domain shift group-visibility rule (ShiftGroupVisibilityFilter.VisibleTo) to a shift query, so the
/// restriction runs in the database; a null restriction (unrestricted caller) leaves the query unchanged.
/// </summary>
/// <param name="query">Shift query the restriction is added to</param>
/// <param name="visibleGroupIds">Flat set of allowed group ids from IShiftGroupFilterService.ResolveShiftListGroupIdsAsync; null = unrestricted</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Shifts;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class ShiftGroupVisibilityQuery
{
    public static IQueryable<Shift> Apply(IQueryable<Shift> query, IReadOnlyCollection<Guid>? visibleGroupIds)
    {
        return visibleGroupIds == null
            ? query
            : query.Where(ShiftGroupVisibilityFilter.VisibleTo(visibleGroupIds));
    }
}
