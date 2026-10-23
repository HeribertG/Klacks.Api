// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Handler for <see cref="GroupUngroupedByCityNameCommand"/>. Loads all employees together with their
/// addresses and group memberships, keeps those without any active (non-scenario) membership, and maps
/// each one to the group whose name exactly matches (case-insensitive, trimmed) the city of its newest
/// employee address. With Apply=false it only previews; with Apply=true it persists the new memberships
/// in a single transaction and re-reads them, rolling everything back on a verification mismatch. Cities
/// without an equally named group and employees without an address are reported back instead of being
/// silently dropped, and group names that are not unique are ignored so no employee is assigned wrongly.
/// The run matches against every group of the installation, so only unrestricted callers (admin, background,
/// installation without groups) may start it; a group-restricted caller is refused before anything is read.
/// </summary>
/// <param name="clientRepository">Loads employees with their addresses and memberships.</param>
/// <param name="groupRepository">Provides the groups whose names are matched against address cities.</param>
/// <param name="groupItemRepository">Reads existing memberships and adds new ones.</param>
/// <param name="unitOfWork">Commits and verifies the new memberships in a single transaction.</param>
/// <param name="groupVisibilityGuard">Tells whether the caller is free of group-visibility restrictions.</param>
/// <param name="startPlanner">Plans the start date of every new membership (one common date, or each person's employment entry) and describes it for the result.</param>
public sealed class GroupUngroupedByCityNameCommandHandler
    : IRequestHandler<GroupUngroupedByCityNameCommand, GroupUngroupedByCityNameResult>
{
    private const string SkillName = "group_ungrouped_by_city_name";
    private const string RestrictedCallerMessage = "Only unrestricted users may group employees across all groups";

    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IGroupMembershipStartPlanner _startPlanner;

    public GroupUngroupedByCityNameCommandHandler(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        IGroupVisibilityGuard groupVisibilityGuard,
        IGroupMembershipStartPlanner startPlanner)
    {
        _startPlanner = startPlanner;
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupUngroupedByCityNameResult> Handle(
        GroupUngroupedByCityNameCommand request, CancellationToken cancellationToken)
    {
        if (!await _groupVisibilityGuard.IsUnrestrictedAsync(cancellationToken))
        {
            throw new ForbiddenException(RestrictedCallerMessage);
        }

        var employees = await _clientRepository.GetByTypeWithAddressesAndGroupItemsAsync(
            EntityTypeEnum.Employee, cancellationToken);

        var ungrouped = employees
            .Where(c => !c.GroupItems.Any(gi => gi.AnalyseToken == null && !gi.IsDeleted))
            .ToList();

        var groups = await _groupRepository.List();
        var groupsByName = BuildUniqueGroupNameIndex(groups);

        var (assignments, unmatchedByCity, noAddress) = MatchByCity(ungrouped, groupsByName);

        if (request.Count is > 0 && assignments.Count > request.Count.Value)
        {
            assignments = assignments.Take(request.Count.Value).ToList();
        }

        var unmatchedCities = OrderUnmatchedCities(unmatchedByCity);

        var now = DateTime.UtcNow;
        var assignedClientIds = assignments.Select(a => a.ClientId).Distinct().ToList();
        var startPlan = await _startPlanner.PlanAsync(
            assignedClientIds, request.ValidFrom, request.StartMode, defaultStart: null, cancellationToken);
        var (existingMembers, newItems) = await SplitExistingFromNewAsync(assignments, startPlan, now, request.UserName);

        var membershipStart = startPlan.Describe(newItems.Select(i => i.ClientId!.Value), existingMembers);

        if (!request.Apply)
        {
            return new GroupUngroupedByCityNameResult(
                Applied: false,
                TotalUngrouped: ungrouped.Count,
                MatchCount: assignments.Count,
                AddedCount: 0,
                VerifiedCount: 0,
                NoAddressCount: noAddress,
                Assignments: assignments,
                UnmatchedCities: unmatchedCities,
                MembershipStart: membershipStart);
        }

        var verified = 0;
        var pulledForward = PullForwardBatch.Empty;
        if (newItems.Count > 0 || membershipStart.ExistingMembersPulledForwardCount > 0)
        {
            (verified, pulledForward) = await PersistAndVerifyAsync(newItems, startPlan, existingMembers, cancellationToken);
        }

        return new GroupUngroupedByCityNameResult(
            Applied: true,
            TotalUngrouped: ungrouped.Count,
            MatchCount: assignments.Count,
            AddedCount: newItems.Count,
            VerifiedCount: verified,
            NoAddressCount: noAddress,
            Assignments: assignments,
            UnmatchedCities: unmatchedCities,
            MembershipStart: pulledForward.ReportOn(membershipStart));
    }

    private static Dictionary<string, Group> BuildUniqueGroupNameIndex(IEnumerable<Group> groups) =>
        groups
            .Where(g => !g.IsDeleted && !string.IsNullOrWhiteSpace(g.Name))
            .GroupBy(g => g.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(byName => byName.Count() == 1)
            .ToDictionary(byName => byName.Key, byName => byName.First(), StringComparer.OrdinalIgnoreCase);

    private static (List<GroupCityAssignment> Assignments, Dictionary<string, int> UnmatchedByCity, int NoAddress) MatchByCity(
        IEnumerable<Client> ungrouped, IReadOnlyDictionary<string, Group> groupsByName)
    {
        var assignments = new List<GroupCityAssignment>();
        var unmatchedByCity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var noAddress = 0;

        foreach (var client in ungrouped)
        {
            var address = client.Addresses
                .Where(a => a.Type == AddressTypeEnum.Employee && !a.IsDeleted)
                .OrderByDescending(a => a.ValidFrom)
                .FirstOrDefault();

            var city = address?.City?.Trim();
            if (string.IsNullOrWhiteSpace(city))
            {
                noAddress++;
                continue;
            }

            if (groupsByName.TryGetValue(city, out var group))
            {
                var name = $"{client.FirstName} {client.Name}".Trim();
                assignments.Add(new GroupCityAssignment(client.Id, name, city, group.Id, group.Name));
            }
            else
            {
                unmatchedByCity[city] = unmatchedByCity.GetValueOrDefault(city) + 1;
            }
        }

        return (assignments, unmatchedByCity, noAddress);
    }

    private static List<UnmatchedCityCount> OrderUnmatchedCities(Dictionary<string, int> unmatchedByCity) =>
        unmatchedByCity
            .Select(kv => new UnmatchedCityCount(kv.Key, kv.Value))
            .OrderByDescending(u => u.EmployeeCount)
            .ThenBy(u => u.City, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private async Task<(List<ExistingMembershipStart> ExistingMembers, List<GroupItem> NewItems)> SplitExistingFromNewAsync(
        List<GroupCityAssignment> assignments, GroupMembershipStartPlan startPlan, DateTime now, string userName)
    {
        var existingMembers = new List<ExistingMembershipStart>();
        var newItems = new List<GroupItem>();

        foreach (var assignment in assignments)
        {
            var existing = await _groupItemRepository.GetByClientAndGroup(assignment.ClientId, assignment.GroupId);
            if (existing != null && !existing.IsDeleted)
            {
                existingMembers.Add(new ExistingMembershipStart(assignment.ClientId, existing.ValidFrom, existing.Id, assignment.GroupId, existing.ValidUntil));
                continue;
            }

            newItems.Add(new GroupItem
            {
                Id = Guid.NewGuid(),
                ClientId = assignment.ClientId,
                GroupId = assignment.GroupId,
                ValidFrom = startPlan.StartFor(assignment.ClientId),
                CreateTime = now,
                CurrentUserCreated = userName
            });
        }

        return (existingMembers, newItems);
    }

    private async Task<(int Verified, PullForwardBatch PulledForward)> PersistAndVerifyAsync(
        List<GroupItem> newItems,
        GroupMembershipStartPlan startPlan,
        List<ExistingMembershipStart> existingMembers,
        CancellationToken cancellationToken)
    {
        var pulledForward = PullForwardBatch.Empty;
        var verified = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var item in newItems)
            {
                await _groupItemRepository.Add(item);
            }

            pulledForward = await _startPlanner.StagePullForwardAsync(startPlan, existingMembers, cancellationToken);
            await _unitOfWork.CompleteAsync();

            var confirmed = newItems.Count == 0
                ? 0
                : await _groupItemRepository.CountExistingByIds(newItems.Select(i => i.Id).ToList(), cancellationToken);
            if (confirmed != newItems.Count)
            {
                throw new SkillVerificationException(
                    SkillName,
                    $"Database verification failed: expected {newItems.Count} new memberships but only " +
                    $"{confirmed} were confirmed — the changes were rolled back.");
            }

            await _startPlanner.VerifyPullForwardAsync(pulledForward, cancellationToken);
            return confirmed;
        });

        return (verified, pulledForward);
    }
}
