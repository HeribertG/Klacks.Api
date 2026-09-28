// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads everything one grouping feasibility analysis needs in a fixed number of queries: all groups,
/// real (non-scenario) group_item rows of clients and shifts, employees and extern employees whose
/// membership overlaps the period with a located address, plannable task shifts (OriginalShift or
/// SplitShift, no scenario copy) overlapping the period with their original and their customer's located
/// address (the same latest-valid-address rule as for clients), their required qualifications, the clients'
/// qualifications, blacklisted shift preferences, hourly availability in the period and the (client,
/// shift) pairs with a real work from today on. group_item validity dates are deliberately not
/// filtered: the plan view and the wizard do not filter them either. Soft-deleted rows are removed by
/// the global query filters. Every query is an internal method so its SQL translation can be proven on
/// the Npgsql provider.
/// </summary>
/// <param name="context">EF context the reads run against, no tracking.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Grouping;

public sealed class GroupingFeasibilityDataSource : IGroupingFeasibilityDataSource
{
    private const string NameSeparator = " ";

    private static readonly ShiftStatus[] PlannableStatuses = [ShiftStatus.OriginalShift, ShiftStatus.SplitShift];
    private static readonly EntityTypeEnum[] PlannableClientTypes = [EntityTypeEnum.Employee, EntityTypeEnum.ExternEmp];

    private readonly DataBaseContext _context;

    public GroupingFeasibilityDataSource(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<GroupingFeasibilitySnapshot> LoadAsync(DateOnly from, DateOnly until, DateOnly today, CancellationToken cancellationToken)
    {
        var fromUtc = ToUtc(from);
        var untilUtc = ToUtc(until);
        var todayUtc = ToUtc(today);

        var groups = await GroupsQuery().ToListAsync(cancellationToken);
        var memberships = await MembershipsQuery().ToListAsync(cancellationToken);
        var clientRows = await ClientsQuery(fromUtc, untilUtc).ToListAsync(cancellationToken);
        var clientIds = clientRows.Select(client => client.Id).ToList();
        var addresses = await AddressesQuery(clientIds, todayUtc).ToListAsync(cancellationToken);
        var shiftRows = await ShiftsQuery(from, until).ToListAsync(cancellationToken);
        var customerIds = shiftRows.Select(shift => shift.CustomerId).OfType<Guid>().Distinct().ToList();
        var customerAddresses = await AddressesQuery(customerIds, todayUtc).ToListAsync(cancellationToken);
        var shiftIds = shiftRows.Select(shift => shift.Id).ToList();
        var requirements = await RequirementsQuery(shiftIds).ToListAsync(cancellationToken);
        var qualifications = await QualificationsQuery(clientIds).ToListAsync(cancellationToken);
        var blacklist = await BlacklistQuery(clientIds).ToListAsync(cancellationToken);
        var availability = await AvailabilityQuery(clientIds, from, until).ToListAsync(cancellationToken);
        var futureWorks = await FutureWorksQuery(clientIds, today).ToListAsync(cancellationToken);

        var locationByClient = LatestLocations(addresses);
        var locationByCustomer = LatestLocations(customerAddresses);
        var shifts = shiftRows
            .Select(shift => shift.CustomerId is Guid customerId && locationByCustomer.TryGetValue(customerId, out var location)
                ? shift with { Latitude = location.Latitude, Longitude = location.Longitude }
                : shift)
            .ToList();
        var clients = clientRows
            .Select(client => locationByClient.TryGetValue(client.Id, out var location)
                ? client with { Latitude = location.Latitude, Longitude = location.Longitude }
                : client)
            .ToList();

        return new GroupingFeasibilitySnapshot(
            groups,
            memberships,
            clients,
            shifts,
            requirements,
            qualifications,
            blacklist.ToHashSet(),
            availability,
            futureWorks.ToHashSet());
    }

    public async Task<bool> HasAnalysableDataAsync(DateOnly today, CancellationToken cancellationToken)
    {
        if (!await ActiveClientQuery(ToUtc(today)).AnyAsync(cancellationToken))
        {
            return false;
        }

        return await GroupedPlannableShiftQuery(today).AnyAsync(cancellationToken);
    }

    internal IQueryable<GroupingGroupRecord> GroupsQuery() =>
        _context.Group.AsNoTracking()
            .Select(group => new GroupingGroupRecord(group.Id, group.Name, group.Parent, group.Root, group.Latitude, group.Longitude));

    internal IQueryable<GroupingMembershipRecord> MembershipsQuery() =>
        _context.GroupItem.AsNoTracking()
            .Where(item => item.AnalyseToken == null && (item.ClientId != null || item.ShiftId != null))
            .Select(item => new GroupingMembershipRecord(item.Id, item.GroupId, item.ClientId, item.ShiftId));

    internal IQueryable<GroupingClientRecord> ClientsQuery(DateTime fromUtc, DateTime untilUtc) =>
        _context.Client.AsNoTracking()
            .Where(client => PlannableClientTypes.Contains(client.Type)
                && client.Membership != null
                && client.Membership.ValidFrom <= untilUtc
                && (client.Membership.ValidUntil == null || client.Membership.ValidUntil >= fromUtc))
            .Select(client => new GroupingClientRecord(
                client.Id,
                ((client.FirstName ?? string.Empty) + NameSeparator + client.Name).Trim(),
                null,
                null));

    internal IQueryable<GroupingAddressRow> AddressesQuery(List<Guid> clientIds, DateTime todayUtc) =>
        _context.Address.AsNoTracking()
            .Where(address => clientIds.Contains(address.ClientId)
                && address.Latitude != null
                && address.Longitude != null
                && (address.ValidFrom == null || address.ValidFrom <= todayUtc))
            .Select(address => new GroupingAddressRow(address.ClientId, address.ValidFrom, address.Latitude, address.Longitude));

    internal IQueryable<GroupingShiftRecord> ShiftsQuery(DateOnly from, DateOnly until) =>
        _context.Shift.AsNoTracking()
            .Where(shift => shift.AnalyseToken == null
                && shift.ScenarioSourceShiftId == null
                && PlannableStatuses.Contains(shift.Status)
                && shift.ShiftType == ShiftType.IsTask
                && shift.FromDate <= until
                && (shift.UntilDate == null || shift.UntilDate >= from))
            .Select(shift => new GroupingShiftRecord(
                shift.Id,
                shift.Name == null || shift.Name == string.Empty ? shift.Abbreviation : shift.Name,
                shift.StartShift,
                shift.EndShift,
                shift.Quantity,
                shift.OriginalId,
                shift.ClientId,
                null,
                null));

    internal IQueryable<ShiftRequiredQualification> RequirementsQuery(List<Guid> shiftIds) =>
        _context.ShiftRequiredQualification.AsNoTracking().Where(requirement => shiftIds.Contains(requirement.ShiftId));

    internal IQueryable<ClientQualification> QualificationsQuery(List<Guid> clientIds) =>
        _context.ClientQualification.AsNoTracking().Where(qualification => clientIds.Contains(qualification.ClientId));

    internal IQueryable<GroupingEntityPair> BlacklistQuery(List<Guid> clientIds) =>
        _context.ClientShiftPreference.AsNoTracking()
            .Where(preference => preference.AnalyseToken == null
                && preference.PreferenceType == ShiftPreferenceType.Blacklist
                && clientIds.Contains(preference.ClientId))
            .Select(preference => new GroupingEntityPair(preference.ClientId, preference.ShiftId));

    internal IQueryable<ClientAvailability> AvailabilityQuery(List<Guid> clientIds, DateOnly from, DateOnly until) =>
        _context.ClientAvailability.AsNoTracking()
            .Where(entry => !entry.IsDeleted && clientIds.Contains(entry.ClientId) && entry.Date >= from && entry.Date <= until);

    internal IQueryable<GroupingEntityPair> FutureWorksQuery(List<Guid> clientIds, DateOnly today) =>
        _context.Work.AsNoTracking()
            .Where(work => work.AnalyseToken == null && work.CurrentDate >= today && clientIds.Contains(work.ClientId))
            .GroupBy(work => new { work.ClientId, work.ShiftId })
            .Select(group => new GroupingEntityPair(group.Key.ClientId, group.Key.ShiftId));

    internal IQueryable<Guid> ActiveClientQuery(DateTime todayUtc) =>
        _context.Client.AsNoTracking()
            .Where(client => PlannableClientTypes.Contains(client.Type)
                && client.Membership != null
                && client.Membership.ValidFrom <= todayUtc
                && (client.Membership.ValidUntil == null || client.Membership.ValidUntil >= todayUtc))
            .Select(client => client.Id);

    internal IQueryable<Guid> GroupedPlannableShiftQuery(DateOnly today) =>
        _context.GroupItem.AsNoTracking()
            .Where(item => item.AnalyseToken == null
                && item.ShiftId != null
                && _context.Shift.Any(shift => shift.Id == item.ShiftId
                    && shift.AnalyseToken == null
                    && shift.ScenarioSourceShiftId == null
                    && PlannableStatuses.Contains(shift.Status)
                    && shift.ShiftType == ShiftType.IsTask
                    && (shift.UntilDate == null || shift.UntilDate >= today)))
            .Select(item => item.Id);

    private static Dictionary<Guid, GroupingAddressRow> LatestLocations(IEnumerable<GroupingAddressRow> addresses) =>
        addresses
            .GroupBy(address => address.ClientId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(address => address.ValidFrom).First());

    private static DateTime ToUtc(DateOnly day) => day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
