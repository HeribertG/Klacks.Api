// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads and groups WorkChange, Expenses and Break rows for a set of Work entries, shared by
/// OrderExportDataLoader and ClientPeriodExportDataLoader to avoid duplicating the query.
/// @param context - Database context used to query WorkChange/Expenses/Break
/// @param workIds - Work identifiers used to look up WorkChange and Expenses rows
/// @param clientIds - Client identifiers used together with workDates to look up Break rows
/// @param workDates - Work dates used together with clientIds to look up Break rows
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Exports;

public static class WorkSubEntryLoader
{
    public static async Task<WorkSubEntryLookups> LoadAsync(
        DataBaseContext context,
        IReadOnlyCollection<Guid> workIds,
        IReadOnlyCollection<Guid> clientIds,
        IReadOnlyCollection<DateOnly> workDates,
        CancellationToken cancellationToken)
    {
        var workChanges = await context.WorkChange
            .AsNoTracking()
            .Where(wc => !wc.IsDeleted && wc.AnalyseToken == null && workIds.Contains(wc.WorkId))
            .Include(wc => wc.ReplaceClient)
            .ToListAsync(cancellationToken);

        var expenses = await context.Expenses
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.AnalyseToken == null && workIds.Contains(e.WorkId))
            .ToListAsync(cancellationToken);

        var breaks = await context.Break
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                && b.AnalyseToken == null
                && clientIds.Contains(b.ClientId)
                && workDates.Contains(b.CurrentDate)
                && b.LockLevel == WorkLockLevel.Closed)
            .Include(b => b.Absence)
            .ToListAsync(cancellationToken);

        return new WorkSubEntryLookups(
            workChanges.GroupBy(wc => wc.WorkId).ToDictionary(g => g.Key, g => g.ToList()),
            expenses.GroupBy(e => e.WorkId).ToDictionary(g => g.Key, g => g.ToList()),
            breaks.GroupBy(b => (b.ClientId, b.CurrentDate)).ToDictionary(g => g.Key, g => g.ToList()));
    }
}
