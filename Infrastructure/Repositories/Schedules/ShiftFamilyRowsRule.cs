// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one query rule for the rows of a family by its head: the head row and every row whose OriginalId points at it.
/// A family without an order is a container, which has no rows pointing at it, so it is the head row alone. The world
/// filters stay at the query.
/// </summary>
/// <param name="headId">Id of the head row: the sealed order, or the container</param>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class ShiftFamilyRowsRule
{
    public static Expression<Func<Shift, bool>> Of(Guid headId) =>
        shift => shift.Id == headId || shift.OriginalId == headId;
}
