// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the ids of every absence type flagged as on-call. Soft-deleted types are included so the breaks of a
/// retired on-call type keep their on-call meaning. Single source for the repository and for the
/// infrastructure validators that read the context directly.
/// </summary>
/// <param name="context">Database context of the caller</param>

using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class OnCallAbsenceQuery
{
    public static async Task<IReadOnlySet<Guid>> LoadIdsAsync(DataBaseContext context, CancellationToken cancellationToken)
    {
        var ids = await context.Absence
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.IsOnCall)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    /// <summary>Ids of the given breaks whose absence type is on-call.</summary>
    /// <param name="breaks">Breaks to classify</param>
    /// <param name="onCallAbsenceIds">Ids of the on-call absence types</param>
    public static IReadOnlySet<Guid> OnCallBreakIds(IEnumerable<Domain.Models.Schedules.Break> breaks, IReadOnlySet<Guid> onCallAbsenceIds)
        => breaks.Where(b => onCallAbsenceIds.Contains(b.AbsenceId)).Select(b => b.Id).ToHashSet();
}
