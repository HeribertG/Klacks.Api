// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Finds work that belongs to the day BEFORE a checked range but runs into its first day - a night shift
/// 31.07 22:00-06:00 seen from a range starting 01.08. The range loaders only load works whose CurrentDate lies
/// inside the range, so without this the 01.08 holiday worked by that shift would be reported by no range at all
/// at a month boundary. Only the holiday-work step uses it; the other checks keep their own load windows.
/// </summary>
/// <param name="dbContext">Scoped database context of the caller</param>
/// <param name="timelineCalculationService">Converts works and work changes into schedule blocks</param>
/// <param name="firstDate">First day of the checked range; works of the day before are inspected</param>
/// <param name="analyseToken">Scenario token; null loads the real schedule only</param>

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class HolidayWorkSpillInLoader
{
    public static async Task<HolidayWorkSpillIn> LoadAsync(
        DataBaseContext dbContext,
        ITimelineCalculationService timelineCalculationService,
        DateOnly firstDate,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var previousDay = firstDate.AddDays(-1);

        var works = await dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Where(w => w.CurrentDate == previousDay && !w.IsDeleted && w.ParentWorkId == null && w.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        if (works.Count == 0)
        {
            return HolidayWorkSpillIn.Empty;
        }

        var workIds = works.Select(w => w.Id).ToList();
        var workChanges = await dbContext.WorkChange
            .AsNoTracking()
            .Include(wc => wc.ReplaceClient)
            .Where(wc => workIds.Contains(wc.WorkId) && !wc.IsDeleted)
            .ToListAsync(cancellationToken);

        var datesByClient = timelineCalculationService
            .CalculateScheduleBlocks(works, workChanges, [])
            .GroupBy(b => b.ClientId)
            .Select(g => (ClientId: g.Key, Dates: WorkedCalendarDates.FromBlocks(g).Where(d => d >= firstDate).ToList()))
            .Where(x => x.Dates.Count > 0)
            .ToDictionary(x => x.ClientId, x => x.Dates);

        return new HolidayWorkSpillIn(datesByClient, ScheduleClientNames.Build(works, workChanges));
    }
}
