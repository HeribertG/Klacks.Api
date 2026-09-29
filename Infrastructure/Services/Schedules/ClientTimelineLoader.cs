// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the timeline of one client over a date window: the client's own top-level works, the works
/// the client covers through a replacement WorkChange, all WorkChanges of those works and the client's
/// breaks, converted to schedule blocks. Used by the live single-day validation wherever a check needs
/// more than the edited day (week-aware checks, cross-day rest periods).
/// </summary>
/// <param name="dbContext">Scoped database context of the caller</param>
/// <param name="timelineCalculationService">Converts works, work changes and breaks into schedule blocks</param>
/// <param name="clientId">Client whose timeline is built</param>
/// <param name="from">First CurrentDate of the load window (inclusive)</param>
/// <param name="to">Last CurrentDate of the load window (inclusive)</param>
/// <param name="analyseToken">Scenario token; null loads the real schedule only</param>
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class ClientTimelineLoader
{
    public static async Task<ClientTimeline> LoadAsync(
        DataBaseContext dbContext,
        ITimelineCalculationService timelineCalculationService,
        Guid clientId,
        DateOnly from,
        DateOnly to,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var ownWorks = await dbContext.Work
            .AsNoTracking()
            .Where(w => w.ClientId == clientId && w.CurrentDate >= from && w.CurrentDate <= to &&
                        !w.IsDeleted && w.ParentWorkId == null && w.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        var worksWithReplacementForClient = await dbContext.Work
            .AsNoTracking()
            .Where(w => w.CurrentDate >= from && w.CurrentDate <= to && !w.IsDeleted &&
                        w.ParentWorkId == null && w.AnalyseToken == analyseToken &&
                        dbContext.WorkChange.Any(wc =>
                            wc.WorkId == w.Id && !wc.IsDeleted && wc.ReplaceClientId == clientId))
            .ToListAsync(cancellationToken);

        var allWorks = ownWorks
            .UnionBy(worksWithReplacementForClient, w => w.Id)
            .ToList();

        var workIds = allWorks.Select(w => w.Id).ToList();
        var workChanges = workIds.Count > 0
            ? await dbContext.WorkChange
                .AsNoTracking()
                .Where(wc => workIds.Contains(wc.WorkId) && !wc.IsDeleted)
                .ToListAsync(cancellationToken)
            : [];

        var breaks = await dbContext.Break
            .AsNoTracking()
            .Where(b => b.ClientId == clientId && b.CurrentDate >= from && b.CurrentDate <= to &&
                        !b.IsDeleted && b.ParentWorkId == null && b.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        var blocks = timelineCalculationService
            .CalculateScheduleBlocks(allWorks, workChanges, breaks)
            .Where(b => b.ClientId == clientId);

        var timeline = new ClientTimeline(clientId);
        timeline.AddBlocks(blocks);
        timeline.SortBlocks();
        return timeline;
    }
}
