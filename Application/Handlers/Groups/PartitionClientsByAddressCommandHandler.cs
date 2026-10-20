// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Geo;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Geo;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Handler for <see cref="PartitionClientsByAddressCommand"/>. Delegates the region/state/city (or
/// cluster) plan to the pure <see cref="GroupPartitionPlanner"/>, then with Apply=false only returns the
/// plan; with Apply=true creates the missing groups (each via <see cref="IGroupRepository.Add"/>,
/// top-down so a parent's nested-set row is committed before its children are inserted, cluster nodes
/// additionally getting their centroid coordinates and GeocodingAttempted set) and the new memberships in
/// a single transaction, committing once and re-reading the memberships to confirm the write. Several
/// client types can be partitioned into the same tree in one call. The run reads and restructures the whole
/// group tree, so only unrestricted callers (admin, background, installation without groups) may start it; a
/// group-restricted caller is refused before anything is read.
/// </summary>
/// <param name="clientRepository">Loads clients of every requested entity type with their addresses and group memberships.</param>
/// <param name="groupRepository">Provides the existing groups and creates the missing ones (nested-set aware).</param>
/// <param name="groupItemRepository">Reads existing memberships and adds new ones.</param>
/// <param name="unitOfWork">Commits and verifies the new memberships in a single transaction.</param>
/// <param name="regionProvider">Supplies the state-to-region map per country; each region reaches the planner with its key, its display name (DEFAULT_LANGUAGE, then English, then the core languages in order) and every language variant of its name.</param>
/// <param name="countryResolver">Supplies the company's default country for addresses without one.</param>
/// <param name="stateRepository">Supplies state display names for the descriptions of state nodes.</param>
/// <param name="settingsReader">Reads the installation-wide DEFAULT_LANGUAGE setting used to pick the language of state and region display names.</param>
/// <param name="visibilityPreservation">Counts the users that keep access to everything when this run introduces the first group, for the advisory the preview shows.</param>
/// <param name="groupVisibilityGuard">Tells whether the caller is free of group-visibility restrictions.</param>
/// <param name="startPlanner">Plans the start date of every new membership (one common date, or each person's employment entry) and describes it for the result.</param>
public sealed class PartitionClientsByAddressCommandHandler
    : IRequestHandler<PartitionClientsByAddressCommand, PartitionClientsByAddressResult>
{
    private const string SkillName = "partition_clients_by_address";
    private const string AllEntityTypesLabel = "All";
    private const int MaxUnassignableSample = 20;
    private const string RestrictedCallerMessage = "Only unrestricted users may partition clients into groups";
    private const string RegionNameFallbackLanguage = "en";

    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICountryRegionProvider _regionProvider;
    private readonly ICountryResolver _countryResolver;
    private readonly IStateRepository _stateRepository;
    private readonly ISettingsReader _settingsReader;
    private readonly IGroupVisibilityPreservationService _visibilityPreservation;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IGroupMembershipStartPlanner _startPlanner;

    public PartitionClientsByAddressCommandHandler(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        ICountryRegionProvider regionProvider,
        ICountryResolver countryResolver,
        IStateRepository stateRepository,
        ISettingsReader settingsReader,
        IGroupVisibilityPreservationService visibilityPreservation,
        IGroupVisibilityGuard groupVisibilityGuard,
        IGroupMembershipStartPlanner startPlanner)
    {
        _groupVisibilityGuard = groupVisibilityGuard;
        _startPlanner = startPlanner;
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _regionProvider = regionProvider;
        _countryResolver = countryResolver;
        _stateRepository = stateRepository;
        _settingsReader = settingsReader;
        _visibilityPreservation = visibilityPreservation;
    }

    public async Task<PartitionClientsByAddressResult> Handle(
        PartitionClientsByAddressCommand request, CancellationToken cancellationToken)
    {
        if (!await _groupVisibilityGuard.IsUnrestrictedAsync(cancellationToken))
        {
            throw new ForbiddenException(RestrictedCallerMessage);
        }

        var clients = new List<Client>();
        foreach (var entityType in request.EntityTypes.Distinct())
        {
            clients.AddRange(await _clientRepository.GetByTypeWithAddressesAndGroupItemsAsync(entityType, cancellationToken));
        }

        var existingGroups = (await _groupRepository.List()).ToList();
        var usersKeepingFullVisibility =
            await _visibilityPreservation.CountUsersKeepingFullVisibilityAsync(cancellationToken);
        var context = await BuildContextAsync(clients, request.ClusterSharePercent, request.SubClusterSharePercent, cancellationToken);

        var plan = GroupPartitionPlanner.Plan(
            clients, existingGroups, request.Level, request.RootGroupId, request.IncludeAlreadyGrouped, context);

        var (plannedNewClientIds, existingMembers) = SplitNewFromExisting(clients, plan);
        var startPlan = await _startPlanner.PlanAsync(
            plan.Assignments.Select(a => a.ClientId).Concat(existingMembers.Select(m => m.ClientId)).Distinct().ToList(),
            request.ValidFrom, request.StartMode, defaultStart: null, cancellationToken);

        if (!request.Apply)
        {
            return BuildResult(
                request, plan, applied: false, groups: plan.Groups, assignedCount: 0, verifiedCount: 0, alreadyMemberCount: 0,
                usersKeepingFullVisibilityCount: usersKeepingFullVisibility,
                membershipStart: startPlan.Describe(plannedNewClientIds, existingMembers));
        }

        var groupStarts = PartitionGroupStarts.Resolve(plan, startPlan);
        var newMemberIds = new List<Guid>();
        var now = DateTime.UtcNow;

        var outcome = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var alreadyMember = 0;
            var resolvedGroupIds = new Dictionary<string, Guid>(StringComparer.Ordinal);

            foreach (var planned in plan.Groups)
            {
                if (planned.Existed)
                {
                    resolvedGroupIds[planned.Key] = planned.ExistingGroupId!.Value;
                    continue;
                }

                var parentId = planned.ParentKey is null ? request.RootGroupId : resolvedGroupIds[planned.ParentKey];
                var hasCoordinates = planned.Latitude.HasValue && planned.Longitude.HasValue;
                var group = new Group
                {
                    Id = Guid.NewGuid(),
                    Name = planned.Name,
                    Description = planned.Description,
                    Parent = parentId,
                    ValidFrom = groupStarts[planned.Key],
                    PaymentInterval = PaymentInterval.Monthly,
                    Latitude = planned.Latitude,
                    Longitude = planned.Longitude,
                    GeocodingAttempted = hasCoordinates,
                    CreateTime = now,
                    CurrentUserCreated = request.UserName
                };

                await _groupRepository.Add(group);
                resolvedGroupIds[planned.Key] = group.Id;
            }

            var newItems = new List<GroupItem>();
            foreach (var assignment in plan.Assignments)
            {
                var groupId = resolvedGroupIds[assignment.LeafGroupKey];
                var existing = await _groupItemRepository.GetByClientAndGroup(assignment.ClientId, groupId);
                if (existing != null && !existing.IsDeleted)
                {
                    alreadyMember++;
                    continue;
                }

                newItems.Add(new GroupItem
                {
                    Id = Guid.NewGuid(),
                    ClientId = assignment.ClientId,
                    GroupId = groupId,
                    ValidFrom = startPlan.StartFor(assignment.ClientId),
                    CreateTime = now,
                    CurrentUserCreated = request.UserName
                });
            }

            if (newItems.Count == 0)
            {
                return new PartitionApplyOutcome(0, alreadyMember, resolvedGroupIds);
            }

            foreach (var item in newItems)
            {
                await _groupItemRepository.Add(item);
            }

            newMemberIds.AddRange(newItems.Select(i => i.ClientId!.Value));

            await _unitOfWork.CompleteAsync();

            var confirmed = await _groupItemRepository.CountExistingByIds(
                newItems.Select(i => i.Id).ToList(), cancellationToken);
            if (confirmed != newItems.Count)
            {
                throw new SkillVerificationException(
                    SkillName,
                    $"Database verification failed: expected {newItems.Count} new memberships but only " +
                    $"{confirmed} were confirmed — the changes were rolled back.");
            }

            return new PartitionApplyOutcome(confirmed, alreadyMember, resolvedGroupIds);
        });

        var createdGroups = plan.Groups
            .Select(g => g with { ExistingGroupId = outcome.ResolvedGroupIds[g.Key] })
            .ToList();
        var assignedCount = plan.Assignments.Count - outcome.AlreadyMemberCount;

        return BuildResult(
            request, plan, applied: true, groups: createdGroups,
            assignedCount: assignedCount, verifiedCount: outcome.VerifiedCount, alreadyMemberCount: outcome.AlreadyMemberCount,
            usersKeepingFullVisibilityCount: usersKeepingFullVisibility,
            membershipStart: startPlan.Describe(newMemberIds, existingMembers));
    }

    private static (List<Guid> NewClientIds, List<ExistingMembershipStart> ExistingMembers) SplitNewFromExisting(
        IReadOnlyList<Client> clients, GroupPartitionPlan plan)
    {
        var clientsById = clients.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());
        var groupsByKey = plan.Groups.ToDictionary(g => g.Key, StringComparer.Ordinal);
        var newClientIds = new List<Guid>();
        var existingMembers = new List<ExistingMembershipStart>();

        foreach (var assignment in plan.Assignments)
        {
            var leaf = groupsByKey[assignment.LeafGroupKey];
            var held = leaf.Existed
                ? ActiveMemberships(clientsById[assignment.ClientId]).FirstOrDefault(gi => gi.GroupId == leaf.ExistingGroupId)
                : null;
            if (held is null)
            {
                newClientIds.Add(assignment.ClientId);
            }
            else
            {
                existingMembers.Add(new ExistingMembershipStart(assignment.ClientId, held.ValidFrom));
            }
        }

        var assignedIds = plan.Assignments.Select(a => a.ClientId).ToHashSet();
        existingMembers.AddRange(clients
            .Where(c => !assignedIds.Contains(c.Id))
            .SelectMany(c => ActiveMemberships(c).Select(gi => new ExistingMembershipStart(c.Id, gi.ValidFrom))));

        return (newClientIds.Distinct().ToList(), existingMembers);
    }

    private static IEnumerable<GroupItem> ActiveMemberships(Client client) =>
        client.GroupItems.Where(gi => !gi.IsDeleted && gi.AnalyseToken == null);

    private async Task<GroupPartitionContext> BuildContextAsync(
        IReadOnlyList<Client> clients, int clusterSharePercent, int subClusterSharePercent, CancellationToken cancellationToken)
    {
        var defaultCountry = NormalizeCountry((await _countryResolver.GetDefaultAsync(cancellationToken))?.Abbreviation);

        var countries = clients
            .Select(c => CustomerGroupingPlanner.SelectPreferredAddress(c, _ => true))
            .Where(a => a != null)
            .Select(a => string.IsNullOrWhiteSpace(a!.Country) ? defaultCountry : NormalizeCountry(a.Country))
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var configuredLanguage = await ResolveConfiguredLanguageAsync();
        var regionNameLanguages = RegionNameLanguages(configuredLanguage);

        var regionByCountry = new Dictionary<string, IReadOnlyDictionary<string, RegionDescriptor>>(StringComparer.OrdinalIgnoreCase);
        foreach (var country in countries)
        {
            var regionByState = await _regionProvider.GetRegionByStateAsync(country, cancellationToken);
            regionByCountry[country] = ToDescriptors(regionByState, regionNameLanguages);
        }

        var countriesInPlay = new HashSet<string>(countries, StringComparer.OrdinalIgnoreCase);
        var stateNameLanguages = StateNameLanguages(configuredLanguage);

        var stateNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var state in await _stateRepository.List())
        {
            var countryPrefix = NormalizeCountry(state.CountryPrefix);
            if (!countriesInPlay.Contains(countryPrefix))
            {
                continue;
            }

            var name = stateNameLanguages
                .Select(language => state.Name?.GetValue(language))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (name != null)
            {
                stateNames[GroupPartitionContext.StateKey(countryPrefix, state.Abbreviation)] = name;
            }
        }

        return new GroupPartitionContext(defaultCountry, regionByCountry, stateNames, clusterSharePercent, subClusterSharePercent);
    }

    private static IReadOnlyDictionary<string, RegionDescriptor> ToDescriptors(
        IReadOnlyDictionary<string, CountryRegion> regionByState, IReadOnlyList<string> languagesInOrder)
    {
        var descriptorByKey = new Dictionary<string, RegionDescriptor>(StringComparer.Ordinal);
        var result = new Dictionary<string, RegionDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var (state, region) in regionByState)
        {
            if (!descriptorByKey.TryGetValue(region.Key, out var descriptor))
            {
                var displayName = languagesInOrder
                    .Select(language => region.Names.GetValue(language))
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
                    ?? region.Names.GetValueOrFirstAvailable(null);
                var allNames = region.Names.ToDictionary().Values
                    .Select(name => name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                descriptor = new RegionDescriptor(region.Key, displayName.Trim(), allNames);
                descriptorByKey[region.Key] = descriptor;
            }

            result[state] = descriptor;
        }

        return result;
    }

    private async Task<string?> ResolveConfiguredLanguageAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.DefaultLanguage);
        return string.IsNullOrWhiteSpace(setting?.Value) ? null : setting.Value.Trim();
    }

    private static IReadOnlyList<string> StateNameLanguages(string? configuredLanguage) =>
        configuredLanguage is null
            ? MultiLanguage.CoreLanguages
            : new[] { configuredLanguage }.Concat(MultiLanguage.CoreLanguages).ToArray();

    private static IReadOnlyList<string> RegionNameLanguages(string? configuredLanguage) =>
        new[] { configuredLanguage, RegionNameFallbackLanguage }
            .Concat(MultiLanguage.CoreLanguages)
            .Where(language => !string.IsNullOrWhiteSpace(language))
            .Select(language => language!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string NormalizeCountry(string? countryCode) =>
        (CountryCodeNormalizer.ToAlpha2(countryCode) ?? string.Empty).Trim().ToUpperInvariant();

    private static PartitionClientsByAddressResult BuildResult(
        PartitionClientsByAddressCommand request,
        GroupPartitionPlan plan,
        bool applied,
        IReadOnlyList<PlannedPartitionGroup> groups,
        int assignedCount,
        int verifiedCount,
        int alreadyMemberCount,
        int usersKeepingFullVisibilityCount,
        GroupMembershipStartInfo membershipStart) =>
        new(
            Applied: applied,
            Level: request.Level.ToString(),
            EntityType: request.EntityTypes.Distinct().Count() == 1 ? request.EntityTypes.First().ToString() : AllEntityTypesLabel,
            TotalClients: plan.TotalClients,
            SkippedAlreadyGroupedCount: plan.SkippedAlreadyGroupedCount,
            UnassignableCount: plan.Unassignable.Count,
            AssignedCount: assignedCount,
            VerifiedCount: verifiedCount,
            AlreadyMemberCount: alreadyMemberCount,
            Groups: BuildSummaries(groups, request.RootGroupName),
            UnassignableSample: plan.Unassignable.Take(MaxUnassignableSample).ToList(),
            Warnings: plan.Warnings,
            UsersKeepingFullVisibilityCount: usersKeepingFullVisibilityCount,
            PlaceAttachments: plan.PlaceAttachments ?? [],
            MembershipStart: membershipStart,
            PopulationCount: plan.PopulationCount,
            UnassignableByReason: CountReasons(plan.Unassignable),
            ClusterPlaceAttachments: plan.ClusterPlaceAttachments ?? [],
            StateThresholds: plan.StateThresholds ?? [],
            Method: PartitionMethodDescriber.Describe(
                request.Level, request.ClusterSharePercent, request.SubClusterSharePercent, plan.StateThresholds ?? []));

    private static List<PartitionReasonCount> CountReasons(IReadOnlyList<UnassignablePartitionClient> unassignable) =>
        unassignable
            .GroupBy(u => u.Reason, StringComparer.Ordinal)
            .Select(g => new PartitionReasonCount(g.Key, g.Count()))
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Reason, StringComparer.Ordinal)
            .ToList();

    private static List<PartitionGroupSummary> BuildSummaries(
        IReadOnlyList<PlannedPartitionGroup> groups, string? rootGroupName)
    {
        var nameByKey = groups.ToDictionary(g => g.Key, g => g.Name, StringComparer.Ordinal);

        return groups
            .Select(g => new PartitionGroupSummary(
                g.Name,
                g.ParentKey is null ? rootGroupName : nameByKey[g.ParentKey],
                g.Existed,
                g.ExistingGroupId,
                g.ClientCount,
                HasCoordinates: g.Latitude.HasValue && g.Longitude.HasValue,
                DirectCount: g.DirectCount,
                AttachedCount: g.AttachedCount,
                AttachedPlaceCount: g.AttachedPlaceCount,
                Method: g.Method))
            .ToList();
    }
}
