// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
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
/// client types can be partitioned into the same tree in one call.
/// </summary>
/// <param name="clientRepository">Loads clients of every requested entity type with their addresses and group memberships.</param>
/// <param name="groupRepository">Provides the existing groups and creates the missing ones (nested-set aware).</param>
/// <param name="groupItemRepository">Reads existing memberships and adds new ones.</param>
/// <param name="unitOfWork">Commits and verifies the new memberships in a single transaction.</param>
/// <param name="companyClock">Supplies the company-local date used when no explicit ValidFrom is given.</param>
/// <param name="regionProvider">Supplies the state-to-region map per country.</param>
/// <param name="countryResolver">Supplies the company's default country for addresses without one.</param>
/// <param name="stateRepository">Supplies state display names for the descriptions of state nodes.</param>
/// <param name="settingsReader">Reads the installation-wide DEFAULT_LANGUAGE setting used to pick the state display name's language.</param>
public sealed class PartitionClientsByAddressCommandHandler
    : IRequestHandler<PartitionClientsByAddressCommand, PartitionClientsByAddressResult>
{
    private const string SkillName = "partition_clients_by_address";
    private const string AllEntityTypesLabel = "All";
    private const int MaxUnassignableSample = 20;

    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyClock _companyClock;
    private readonly ICountryRegionProvider _regionProvider;
    private readonly ICountryResolver _countryResolver;
    private readonly IStateRepository _stateRepository;
    private readonly ISettingsReader _settingsReader;

    public PartitionClientsByAddressCommandHandler(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        ICompanyClock companyClock,
        ICountryRegionProvider regionProvider,
        ICountryResolver countryResolver,
        IStateRepository stateRepository,
        ISettingsReader settingsReader)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _companyClock = companyClock;
        _regionProvider = regionProvider;
        _countryResolver = countryResolver;
        _stateRepository = stateRepository;
        _settingsReader = settingsReader;
    }

    public async Task<PartitionClientsByAddressResult> Handle(
        PartitionClientsByAddressCommand request, CancellationToken cancellationToken)
    {
        var clients = new List<Client>();
        foreach (var entityType in request.EntityTypes.Distinct())
        {
            clients.AddRange(await _clientRepository.GetByTypeWithAddressesAndGroupItemsAsync(entityType, cancellationToken));
        }

        var existingGroups = (await _groupRepository.List()).ToList();
        var context = await BuildContextAsync(clients, request.ClusterSharePercent, cancellationToken);

        var plan = GroupPartitionPlanner.Plan(
            clients, existingGroups, request.Level, request.RootGroupId, request.IncludeAlreadyGrouped, context);

        if (!request.Apply)
        {
            return BuildResult(
                request, plan, applied: false, groups: plan.Groups, assignedCount: 0, verifiedCount: 0, alreadyMemberCount: 0);
        }

        var validFrom = request.ValidFrom ?? await _companyClock.GetTodayAsync(cancellationToken);
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
                    ValidFrom = validFrom,
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
                    ValidFrom = validFrom,
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
            assignedCount: assignedCount, verifiedCount: outcome.VerifiedCount, alreadyMemberCount: outcome.AlreadyMemberCount);
    }

    private async Task<GroupPartitionContext> BuildContextAsync(
        IReadOnlyList<Client> clients, int clusterSharePercent, CancellationToken cancellationToken)
    {
        var defaultCountry = (await _countryResolver.GetDefaultAsync(cancellationToken))?.Abbreviation?.Trim().ToUpperInvariant()
            ?? string.Empty;

        var countries = clients
            .Select(c => CustomerGroupingPlanner.SelectPreferredAddress(c, _ => true))
            .Where(a => a != null)
            .Select(a => string.IsNullOrWhiteSpace(a!.Country) ? defaultCountry : a.Country.Trim().ToUpperInvariant())
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var regionByCountry = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var country in countries)
        {
            regionByCountry[country] = await _regionProvider.GetRegionByStateAsync(country, cancellationToken);
        }

        var countriesInPlay = new HashSet<string>(countries, StringComparer.OrdinalIgnoreCase);
        var languagesInOrder = await ResolveStateNameLanguagesAsync();

        var stateNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var state in await _stateRepository.List())
        {
            var countryPrefix = state.CountryPrefix.Trim().ToUpperInvariant();
            if (!countriesInPlay.Contains(countryPrefix))
            {
                continue;
            }

            var name = languagesInOrder
                .Select(language => state.Name?.GetValue(language))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (name != null)
            {
                stateNames[GroupPartitionContext.StateKey(countryPrefix, state.Abbreviation)] = name;
            }
        }

        return new GroupPartitionContext(defaultCountry, regionByCountry, stateNames, clusterSharePercent);
    }

    private async Task<IReadOnlyList<string>> ResolveStateNameLanguagesAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.DefaultLanguage);
        var configured = setting?.Value;

        return string.IsNullOrWhiteSpace(configured)
            ? MultiLanguage.CoreLanguages
            : new[] { configured }.Concat(MultiLanguage.CoreLanguages).ToArray();
    }

    private static PartitionClientsByAddressResult BuildResult(
        PartitionClientsByAddressCommand request,
        GroupPartitionPlan plan,
        bool applied,
        IReadOnlyList<PlannedPartitionGroup> groups,
        int assignedCount,
        int verifiedCount,
        int alreadyMemberCount) =>
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
            Warnings: plan.Warnings);

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
                g.ClientCount))
            .ToList();
    }
}
