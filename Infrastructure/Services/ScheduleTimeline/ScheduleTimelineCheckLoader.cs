// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read side of the schedule timeline checks: loads the rows of a single (client/day) or range check, the
/// per-client timelines and holiday spill-in the checks need beyond them, and the shift locations of a range check.
/// Scoped, because it reads through the check scope's DataBaseContext.
/// </summary>
/// <param name="dbContext">The DataBaseContext of the current check attempt</param>
/// <param name="timelineCalculationService">Turns loaded rows into schedule blocks</param>

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Services.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.ScheduleTimeline;

public sealed class ScheduleTimelineCheckLoader
{
    private readonly DataBaseContext _dbContext;
    private readonly ITimelineCalculationService _timelineCalculationService;

    public ScheduleTimelineCheckLoader(DataBaseContext dbContext, ITimelineCalculationService timelineCalculationService)
    {
        _dbContext = dbContext;
        _timelineCalculationService = timelineCalculationService;
    }

    public async Task<TimelineCheckRows> LoadDayAsync(Guid clientId, DateOnly date, Guid? analyseToken, CancellationToken cancellationToken)
    {
        var ownWorks = await _dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Include(w => w.Shift).ThenInclude(s => s!.Client).ThenInclude(c => c!.Addresses)
            .Where(w => w.ClientId == clientId && w.CurrentDate == date && !w.IsDeleted && w.ParentWorkId == null && w.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        var worksWithReplacementForClient = await _dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Include(w => w.Shift).ThenInclude(s => s!.Client).ThenInclude(c => c!.Addresses)
            .Where(w => w.CurrentDate == date && !w.IsDeleted && w.ParentWorkId == null && w.AnalyseToken == analyseToken &&
                        _dbContext.WorkChange.Any(wc =>
                            wc.WorkId == w.Id && !wc.IsDeleted && wc.ReplaceClientId == clientId))
            .ToListAsync(cancellationToken);

        var allWorks = ownWorks
            .Union(worksWithReplacementForClient, new WorkIdComparer())
            .ToList();

        var workChanges = await LoadWorkChangesAsync(allWorks, cancellationToken);

        var breaks = await _dbContext.Break
            .AsNoTracking()
            .Where(b => b.ClientId == clientId && b.CurrentDate == date && !b.IsDeleted && b.ParentWorkId == null && b.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        return new TimelineCheckRows(ownWorks, allWorks, workChanges, breaks, await LoadOnCallBreakIdsAsync(breaks, cancellationToken));
    }

    public async Task<TimelineCheckRows> LoadRangeAsync(DateOnly startDate, DateOnly endDate, Guid? analyseToken, CancellationToken cancellationToken)
    {
        var works = await _dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Where(w => w.CurrentDate >= startDate && w.CurrentDate <= endDate && !w.IsDeleted && w.ParentWorkId == null && w.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        var workChanges = await LoadWorkChangesAsync(works, cancellationToken);

        var breaks = await _dbContext.Break
            .AsNoTracking()
            .Where(b => b.CurrentDate >= startDate && b.CurrentDate <= endDate && !b.IsDeleted && b.ParentWorkId == null && b.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        return new TimelineCheckRows(works, works, workChanges, breaks, await LoadOnCallBreakIdsAsync(breaks, cancellationToken));
    }

    public Task<ClientTimeline> LoadClientTimelineAsync(
        Guid clientId, DateOnly from, DateOnly to, Guid? analyseToken, CancellationToken cancellationToken)
        => ClientTimelineLoader.LoadAsync(
            _dbContext, _timelineCalculationService, clientId, from, to, analyseToken, cancellationToken);

    public Task<HolidayWorkSpillIn> LoadHolidaySpillInAsync(DateOnly firstDate, Guid? analyseToken, CancellationToken cancellationToken)
        => HolidayWorkSpillInLoader.LoadAsync(_dbContext, _timelineCalculationService, firstDate, analyseToken, cancellationToken);

    /// <summary>
    /// Loads the shift locations for a range check. The single check gets them through its work query's
    /// includes, but a range check spans every client of the period — including the shift/client/address
    /// graph there would weigh down the main query, so the addresses are fetched once for the distinct
    /// shifts instead.
    /// </summary>
    public async Task<Dictionary<Guid, Address?>> LoadShiftAddressLookupAsync(List<Work> works, CancellationToken cancellationToken)
    {
        var shiftIds = works
            .Select(w => w.ShiftId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (shiftIds.Count == 0) return [];

        var shifts = await _dbContext.Shift
            .AsNoTracking()
            .Include(s => s.Client).ThenInclude(c => c!.Addresses)
            .Where(s => shiftIds.Contains(s.Id) && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        var lookup = new Dictionary<Guid, Address?>();
        foreach (var shift in shifts)
        {
            lookup[shift.Id] = TimelineTravelTimeEntries.LatestAddress(shift.Client);
        }
        return lookup;
    }

    private async Task<List<WorkChange>> LoadWorkChangesAsync(List<Work> works, CancellationToken cancellationToken)
    {
        var workIds = works.Select(w => w.Id).ToList();
        return workIds.Count > 0
            ? await _dbContext.WorkChange
                .AsNoTracking()
                .Include(wc => wc.ReplaceClient)
                .Where(wc => workIds.Contains(wc.WorkId) && !wc.IsDeleted)
                .ToListAsync(cancellationToken)
            : [];
    }

    private async Task<IReadOnlySet<Guid>> LoadOnCallBreakIdsAsync(List<Break> breaks, CancellationToken cancellationToken)
        => OnCallAbsenceQuery.OnCallBreakIds(breaks, await OnCallAbsenceQuery.LoadIdsAsync(_dbContext, cancellationToken));
}
