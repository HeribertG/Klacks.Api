// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the inputs of company-holiday detection for one root subtree (Root group plus every group whose Root
/// is that id). Only real-plan rows count: a GroupItem with AnalyseToken or ScenarioSourceGroupItemId belongs
/// to a scenario, a Break with an AnalyseToken too. Soft-deleted GroupItems are excluded explicitly because
/// GroupItem carries no global query filter; deleted memberships and employees are skipped as well. Both queries filter on the entities themselves before projecting,
/// the shape proven translatable by Npgsql (see ShiftGroupScopeReadRepository).
/// The full-day test is done in memory with TimeRange.IsFullDayMarker so the definition of a full-day booking
/// stays in one place; the database only pre-filters on the midnight start.
/// </summary>
/// <param name="context">EF Core context holding Group, GroupItem, Membership and Break.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.ValueObjects;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class GroupAbsenceReadRepository : IGroupAbsenceReadRepository
{
    private readonly DataBaseContext _context;

    public GroupAbsenceReadRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GroupMembershipWindow>> GetMembershipWindowsAsync(
        Guid rootId,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var untilUtc = until.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var rows = await (
                from groupItem in _context.GroupItem
                join subtreeGroup in _context.Group on groupItem.GroupId equals subtreeGroup.Id
                join membership in _context.Membership on groupItem.ClientId equals membership.ClientId
                where groupItem.ClientId != null
                    && !groupItem.IsDeleted
                    && groupItem.AnalyseToken == null
                    && groupItem.ScenarioSourceGroupItemId == null
                    && !subtreeGroup.IsDeleted
                    && !membership.IsDeleted
                    && !membership.Client.IsDeleted
                    && (subtreeGroup.Id == rootId || subtreeGroup.Root == rootId)
                    && membership.ValidFrom <= untilUtc
                    && (membership.ValidUntil == null || membership.ValidUntil >= fromUtc)
                    && (groupItem.ValidFrom == null || groupItem.ValidFrom <= untilUtc)
                    && (groupItem.ValidUntil == null || groupItem.ValidUntil >= fromUtc)
                select new
                {
                    ClientId = groupItem.ClientId!.Value,
                    membership.ValidFrom,
                    MembershipUntil = membership.ValidUntil,
                    GroupItemFrom = groupItem.ValidFrom,
                    GroupItemUntil = groupItem.ValidUntil
                })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new GroupMembershipWindow(
                row.ClientId,
                DateOnly.FromDateTime(row.ValidFrom),
                ToDate(row.MembershipUntil),
                ToDate(row.GroupItemFrom),
                ToDate(row.GroupItemUntil)))
            .Distinct()
            .ToList();
    }

    public async Task<IReadOnlyList<ClientFullDayAbsence>> GetFullDayAbsencesAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken = default)
    {
        if (clientIds.Count == 0)
        {
            return Array.Empty<ClientFullDayAbsence>();
        }

        var distinctClientIds = clientIds.Distinct().ToList();
        var midnight = DayTimeConstants.Midnight;

        var rows = await _context.Break
            .Where(absence => distinctClientIds.Contains(absence.ClientId)
                && !absence.IsDeleted
                && absence.AnalyseToken == null
                && absence.CurrentDate >= from
                && absence.CurrentDate <= until
                && absence.StartTime == midnight)
            .Select(absence => new { absence.ClientId, absence.CurrentDate, absence.StartTime, absence.EndTime })
            .ToListAsync(cancellationToken);

        return rows
            .Where(row => TimeRange.ForAbsenceRecording(row.StartTime, row.EndTime).IsFullDayMarker)
            .Select(row => new ClientFullDayAbsence(row.ClientId, row.CurrentDate))
            .Distinct()
            .ToList();
    }

    private static DateOnly? ToDate(DateTime? value) => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
}
