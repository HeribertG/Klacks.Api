// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only diagnostic for vague requests like "sort the employees into groups": inspects the
/// groups visible to the caller and forms hypotheses about the semantic criterion behind their
/// names (canton, city, qualification, contract or geo-coordinates), plus how many active
/// members each group has and how many clients of the given entity type are not yet grouped at
/// all. Always call this FIRST when the user wants clients sorted/distributed into groups without
/// naming a concrete criterion — the result instructs the model to ask a concrete follow-up
/// question naming the dominant hypothesis instead of guessing.
/// </summary>
/// <param name="entityType">Optional client type the ungrouped count is computed for; defaults to Employee.</param>

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("analyze_group_semantics")]
public class AnalyzeGroupSemanticsSkill : BaseSkillImplementation
{
    private const int MaxExampleGroupNames = 5;
    private const string EntityTypeParameter = "entityType";
    private const string NoGroupsMessage =
        "There are no groups in your scope yet. Ask the user whether groups should be created first " +
        "(e.g. by canton, city, qualification or contract) before sorting clients into them.";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IQualificationRepository _qualificationRepository;
    private readonly IMediator _mediator;

    public AnalyzeGroupSemanticsSkill(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IGroupItemRepository groupItemRepository,
        IClientRepository clientRepository,
        IContractRepository contractRepository,
        IQualificationRepository qualificationRepository,
        IMediator mediator)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _groupItemRepository = groupItemRepository;
        _clientRepository = clientRepository;
        _contractRepository = contractRepository;
        _qualificationRepository = qualificationRepository;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var entityType = ParseEntityType(GetParameter<string>(parameters, EntityTypeParameter));

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var groups = scope.Filter(await _groupRepository.List())
            .Where(g => !g.IsDeleted && !string.IsNullOrWhiteSpace(g.Name))
            .ToList();

        var clientsOfType = await _clientRepository.GetByTypeWithAddressesAndGroupItemsAsync(
            entityType, cancellationToken);
        var ungroupedCount = clientsOfType.Count(c => !HasActiveMembership(c.GroupItems));

        if (groups.Count == 0)
        {
            var emptyResult = new AnalyzeGroupSemanticsResult(
                0, [], [], entityType.ToString(), ungroupedCount, clientsOfType.Count);
            return SkillResult.SuccessResult(emptyResult, NoGroupsMessage);
        }

        var referenceData = await BuildReferenceDataAsync(cancellationToken);

        var categoriesByGroup = groups.ToDictionary(
            g => g.Id,
            g => GroupSemanticsClassifier.Classify(g.Name, HasCoordinates(g), referenceData));

        var categorySummaries = BuildCategorySummaries(groups, categoriesByGroup);
        var memberCounts = await BuildMemberCountsAsync(groups, cancellationToken);

        var result = new AnalyzeGroupSemanticsResult(
            groups.Count,
            categorySummaries,
            memberCounts,
            entityType.ToString(),
            ungroupedCount,
            clientsOfType.Count);

        return SkillResult.SuccessResult(result, BuildMessage(result));
    }

    private async Task<GroupSemanticsReferenceData> BuildReferenceDataAsync(CancellationToken cancellationToken)
    {
        var states = (await _mediator.Send(new ListQuery<StateResource>(), cancellationToken)).ToList();
        var cantonTokens = BuildCantonTokens(states);

        var clientsWithAddresses = await _clientRepository.GetActiveClientsWithAddressesAsync(cancellationToken);
        var cityNames = BuildCityNames(clientsWithAddresses);

        var qualifications = await _qualificationRepository.GetAllAsync(cancellationToken);
        var qualificationNames = BuildQualificationNames(qualifications);

        var contracts = await _contractRepository.List();
        var contractNames = contracts
            .Where(c => !c.IsDeleted && !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => c.Name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new GroupSemanticsReferenceData(cantonTokens, cityNames, qualificationNames, contractNames);
    }

    private static HashSet<string> BuildCantonTokens(IEnumerable<StateResource> states)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var state in states)
        {
            if (!string.IsNullOrWhiteSpace(state.Abbreviation))
            {
                tokens.Add(state.Abbreviation.Trim());
            }

            var nameValues = state.Name?.ToDictionary().Values ?? Enumerable.Empty<string>();
            foreach (var value in nameValues)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    tokens.Add(value.Trim());
                }
            }
        }

        return tokens;
    }

    private static HashSet<string> BuildCityNames(IEnumerable<Client> clients)
    {
        var cities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var client in clients)
        {
            foreach (var address in client.Addresses.Where(a => !a.IsDeleted))
            {
                if (!string.IsNullOrWhiteSpace(address.City))
                {
                    cities.Add(address.City.Trim());
                }
            }
        }

        return cities;
    }

    private static HashSet<string> BuildQualificationNames(IEnumerable<Qualification> qualifications)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var qualification in qualifications)
        {
            foreach (var value in qualification.Name.ToDictionary().Values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    names.Add(value.Trim());
                }
            }
        }

        return names;
    }

    private static bool HasCoordinates(Group group) => group.Latitude.HasValue && group.Longitude.HasValue;

    private static bool HasActiveMembership(IEnumerable<GroupItem> groupItems) =>
        groupItems.Any(gi => !gi.IsDeleted && gi.AnalyseToken == null);

    private static List<GroupSemanticCategorySummary> BuildCategorySummaries(
        List<Group> groups, Dictionary<Guid, IReadOnlySet<GroupSemanticCategory>> categoriesByGroup)
    {
        return Enum.GetValues<GroupSemanticCategory>()
            .Select(category =>
            {
                var matching = groups.Where(g => categoriesByGroup[g.Id].Contains(category)).ToList();
                return new GroupSemanticCategorySummary(
                    category,
                    matching.Count,
                    matching.Select(g => g.Name).Take(MaxExampleGroupNames).ToList());
            })
            .Where(summary => summary.Count > 0)
            .OrderByDescending(summary => summary.Count)
            .ToList();
    }

    private async Task<List<GroupMemberCount>> BuildMemberCountsAsync(
        List<Group> groups, CancellationToken cancellationToken)
    {
        var groupIds = groups.Select(g => g.Id).ToList();
        var activeMemberGroupIds = await _groupItemRepository.GetQuery()
            .Where(gi => groupIds.Contains(gi.GroupId) && gi.ClientId != null && gi.AnalyseToken == null)
            .Select(gi => gi.GroupId)
            .ToListAsync(cancellationToken);

        var countsByGroupId = activeMemberGroupIds
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        return groups
            .Select(g => new GroupMemberCount(g.Id, g.Name, countsByGroupId.GetValueOrDefault(g.Id)))
            .OrderByDescending(m => m.ActiveMemberCount)
            .ToList();
    }

    private static EntityTypeEnum ParseEntityType(string? value) => value switch
    {
        "ExternEmp" => EntityTypeEnum.ExternEmp,
        "Customer" => EntityTypeEnum.Customer,
        _ => EntityTypeEnum.Employee
    };

    private static string BuildMessage(AnalyzeGroupSemanticsResult result)
    {
        var dominant = result.CategorySummaries
            .Where(s => s.Category != GroupSemanticCategory.Other)
            .ToList();

        var ungroupedText =
            $"{result.UngroupedCount} of {result.TotalClientCountForType} {result.UngroupedEntityType}(s) " +
            "have no active group membership.";

        if (dominant.Count == 0)
        {
            return $"None of the {result.TotalGroupCount} group name(s) in scope match a canton, city, " +
                "qualification or contract, and none carry geo-coordinates. Do not guess a grouping criterion " +
                $"— ask the user directly what it should be. {ungroupedText}";
        }

        var hypothesisText = string.Join("; ", dominant.Select(s =>
            $"{s.Category} ({s.Count} group(s), e.g. {string.Join(", ", s.ExampleGroupNames)})"));

        return $"Found {result.TotalGroupCount} group(s) in scope. Likely grouping hypothesis: {hypothesisText}. " +
            $"{ungroupedText} Do NOT guess — ask the user a concrete question naming the dominant hypothesis and " +
            "the actual group names as options. Only after the user confirms a criterion: use " +
            "fill_group_by_criteria (apply=false preview first; supports canton, city, zipPrefix, " +
            "qualificationName, contractName and entityType — one call per target group), " +
            "group_ungrouped_by_city_name to bulk-sort ungrouped clients into city-named groups, " +
            "add_client_to_nearest_group to place a single client into its nearest geo-coordinate group, or " +
            "propose_employee_grouping for a bulk geographic re-distribution dry run.";
    }
}
