// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The span the recalculation of all unsealed entries walks: from the first to the last day of every unsealed real-plan
/// work, every unsealed break and every cached real-plan period-hours row, so the rows of fully sealed periods and rows
/// stored with other period boundaries are rewritten as well. Null days when there is nothing at all.
/// </summary>
/// <param name="context">Database context of the caller's scope</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

internal static class AllUnsealedSpanQuery
{
    public static async Task<(DateOnly? First, DateOnly? Last)> LoadAsync(
        DataBaseContext context, CancellationToken cancellationToken)
    {
        var works = context.Work.Where(w => !w.IsDeleted && w.LockLevel == WorkLockLevel.None && w.AnalyseToken == null);
        var breaks = context.Break.Where(b => !b.IsDeleted && b.LockLevel == WorkLockLevel.None);
        var cachedRows = context.ClientPeriodHours.Where(p => p.AnalyseToken == null);

        var firstWork = await works.MinAsync(w => (DateOnly?)w.CurrentDate, cancellationToken);
        var lastWork = await works.MaxAsync(w => (DateOnly?)w.CurrentDate, cancellationToken);
        var firstBreak = await breaks.MinAsync(b => (DateOnly?)b.CurrentDate, cancellationToken);
        var lastBreak = await breaks.MaxAsync(b => (DateOnly?)b.CurrentDate, cancellationToken);
        var firstCachedDay = await cachedRows.MinAsync(p => (DateOnly?)p.StartDate, cancellationToken);
        var lastCachedDay = await cachedRows.MaxAsync(p => (DateOnly?)p.EndDate, cancellationToken);

        return (
            EarlierOf(EarlierOf(firstWork, firstBreak), firstCachedDay),
            LaterOf(LaterOf(lastWork, lastBreak), lastCachedDay));
    }

    private static DateOnly? EarlierOf(DateOnly? left, DateOnly? right) =>
        left is null ? right : right is null ? left : left < right ? left : right;

    private static DateOnly? LaterOf(DateOnly? left, DateOnly? right) =>
        left is null ? right : right is null ? left : left > right ? left : right;
}
