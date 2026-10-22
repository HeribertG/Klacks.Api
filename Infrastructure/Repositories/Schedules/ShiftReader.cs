// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

/// <summary>
/// Materialising shift reads of the Application layer (proactive scans, order lineage of the cut skills,
/// find_split_shift_candidates, Shifts/ByIds). Each one composes on ShiftBaseQueries, the same base query
/// ShiftRepository.GetQuery() uses, so the base order (original id, from date, start time) and the no-tracking load
/// are the same as when the callers composed IShiftRepository.GetQuery() themselves.
/// </summary>
/// <param name="context">Database context the shifts are read from</param>
public class ShiftReader : IShiftReader
{
    private readonly DataBaseContext _context;

    public ShiftReader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Shift>> ScanRealPlanAsync(
        RealPlanShiftScan scan, CancellationToken cancellationToken = default) =>
        await BuildRealPlanScan(scan).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShiftDateKey>> ScanRealPlanKeysAsync(
        RealPlanShiftScan scan, CancellationToken cancellationToken = default) =>
        await BuildRealPlanScan(scan)
            .Select(s => new ShiftDateKey(s.Id, s.FromDate))
            .ToListAsync(cancellationToken);

    public Task<Shift?> FindFirstByOriginalIdAsync(
        Guid originalId, ShiftStatus status, CancellationToken cancellationToken = default) =>
        Ordered().FirstOrDefaultAsync(s => s.OriginalId == originalId && s.Status == status, cancellationToken);

    public Task<int> CountByOriginalIdAsync(
        Guid originalId, ShiftStatus status, CancellationToken cancellationToken = default) =>
        Ordered().CountAsync(s => s.OriginalId == originalId && s.Status == status, cancellationToken);

    public Task<bool> AnyByOriginalIdAsync(
        Guid originalId, ShiftStatus status, CancellationToken cancellationToken = default) =>
        Ordered().AnyAsync(s => s.OriginalId == originalId && s.Status == status, cancellationToken);

    public async Task<IReadOnlyList<Shift>> GetOrderFamilyShiftsInGroupAsync(
        Guid groupId, string? nameFragment, CancellationToken cancellationToken = default)
    {
        var query = Ordered()
            .Where(s => s.GroupItems.Any(gi => gi.GroupId == groupId && !gi.IsDeleted))
            .Where(s => s.Status == ShiftStatus.SealedOrder
                        || s.Status == ShiftStatus.OriginalShift
                        || s.Status == ShiftStatus.SplitShift);

        if (!string.IsNullOrWhiteSpace(nameFragment))
        {
            query = query.Where(s => EF.Functions.ILike(s.Name, $"%{nameFragment}%"));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shift>> GetVisibleRealPlanWithClientByIdsAsync(
        IReadOnlyCollection<Guid> shiftIds,
        IReadOnlyCollection<Guid>? visibleGroupIds,
        CancellationToken cancellationToken = default)
    {
        var ids = shiftIds.ToList();
        var query = ShiftBaseQueries.OrderedWithClient(_context.Shift)
            .Where(s => ids.Contains(s.Id)
                && s.AnalyseToken == null
                && s.ScenarioSourceShiftId == null);

        return await ShiftGroupVisibilityFilter.Apply(query, visibleGroupIds).ToListAsync(cancellationToken);
    }

    private IQueryable<Shift> Ordered() => ShiftBaseQueries.Ordered(_context.Shift);

    private IQueryable<Shift> BuildRealPlanScan(RealPlanShiftScan scan)
    {
        var status = scan.Status;
        var query = Ordered()
            .Where(s => s.Status == status
                && s.AnalyseToken == null
                && s.ScenarioSourceShiftId == null
                && !s.IsDeleted);

        if (scan.ShiftType is { } shiftType)
        {
            query = query.Where(s => s.ShiftType == shiftType);
        }

        if (scan.StartsOnOrAfter is { } firstDay)
        {
            query = query.Where(s => s.FromDate >= firstDay);
        }

        if (scan.NotEndedBefore is { } runningOn)
        {
            query = query.Where(s => s.UntilDate == null || s.UntilDate >= runningOn);
        }

        if (scan.FullDayOnly)
        {
            query = query.Where(s => s.StartShift == s.EndShift);
        }

        if (scan.OnlyIds is not null)
        {
            var onlyIds = scan.OnlyIds.ToList();
            query = query.Where(s => onlyIds.Contains(s.Id));
        }

        if (scan.ExcludedIds is not null)
        {
            var excludedIds = scan.ExcludedIds.ToList();
            query = query.Where(s => !excludedIds.Contains(s.Id));
        }

        if (scan.OrderByFromDateThenId)
        {
            query = query.OrderBy(s => s.FromDate).ThenBy(s => s.Id);
        }

        if (scan.MaxCount is { } maxCount)
        {
            query = query.Take(maxCount);
        }

        return query;
    }
}
