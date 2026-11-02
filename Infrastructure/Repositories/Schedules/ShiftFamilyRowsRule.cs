// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one query rule for the rows of a family by its head: the head row, every row whose OriginalId points at it and,
/// without an order, every row whose RootId points at it. A row with an order is never matched through RootId, because
/// RootId follows two conventions there (order root in the seed, self root in a live cut). The world filters stay at the query.
/// </summary>
/// <param name="headId">Id of the head row: the sealed order, or the first shift of a family without an order</param>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class ShiftFamilyRowsRule
{
    public static Expression<Func<Shift, bool>> Of(Guid headId) =>
        shift => shift.Id == headId || shift.OriginalId == headId || (shift.OriginalId == null && shift.RootId == headId);
}
