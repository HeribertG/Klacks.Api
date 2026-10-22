// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

/// <summary>
/// The one definition of the shift base query that ShiftRepository and ShiftReader compose on: the built-in order
/// (original id, from date, start time) and no tracking, optionally with the client and its addresses.
/// </summary>
internal static class ShiftBaseQueries
{
    /// <param name="shifts">The shift set to read from (the context's Shift DbSet)</param>
    public static IQueryable<Shift> Ordered(IQueryable<Shift> shifts) =>
        shifts
            .OrderBy(x => x.OriginalId)
            .ThenBy(x => x.FromDate)
            .ThenBy(x => x.StartShift)
            .AsNoTracking();

    /// <param name="shifts">The shift set to read from (the context's Shift DbSet)</param>
    public static IQueryable<Shift> OrderedWithClient(IQueryable<Shift> shifts) =>
        Ordered(shifts
            .Include(x => x.Client!)
            .ThenInclude(x => x.Addresses!.Where(a => a != null)));
}
