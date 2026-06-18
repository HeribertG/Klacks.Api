// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fills a group (by name) with the clients that match a set of criteria — canton/region, an active
/// contract (by name) and entity type — in a single server-side round trip. With apply=false (default)
/// it returns a read-only preview of who would be added; with apply=true it persists the memberships.
/// </summary>
/// <param name="groupName">Name (or partial name) of the target group.</param>
/// <param name="canton">Optional canton/state code the client's address must match (e.g. 'BE').</param>
/// <param name="contractName">Optional name (or partial name) of an active contract the client must hold.</param>
/// <param name="entityType">Optional client type (Employee, ExternEmp, Customer); defaults to Employee.</param>
/// <param name="count">Optional maximum number of clients to add.</param>
/// <param name="apply">When true the matches are added; when false (default) only a preview is returned.</param>

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("fill_group_by_criteria")]
public class FillGroupByCriteriaSkill : BaseSkillImplementation
{
    private readonly IGroupRepository _groupRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IMediator _mediator;

    public FillGroupByCriteriaSkill(
        IGroupRepository groupRepository,
        IContractRepository contractRepository,
        IMediator mediator)
    {
        _groupRepository = groupRepository;
        _contractRepository = contractRepository;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetRequiredString(parameters, "groupName");
        var canton = GetParameter<string>(parameters, "canton");
        var contractName = GetParameter<string>(parameters, "contractName");
        var entityTypeValue = GetParameter<string>(parameters, "entityType");
        var count = GetParameter<int?>(parameters, "count");
        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        var groups = await _groupRepository.List();
        var group = groups.FirstOrDefault(g => !g.IsDeleted &&
            g.Name.Contains(groupName, StringComparison.OrdinalIgnoreCase));
        if (group == null)
        {
            var availableGroups = groups.Where(g => !g.IsDeleted).Select(g => g.Name).ToList();
            var available = availableGroups.Count > 0
                ? "Available groups: " + string.Join(", ", availableGroups) + "."
                : "There are no groups yet.";
            return SkillResult.Error(
                $"Group '{groupName}' not found. {available} " +
                "Offer the user only these real group names — do not invent groups.");
        }

        Guid? contractId = null;
        if (!string.IsNullOrWhiteSpace(contractName))
        {
            var allContracts = await _contractRepository.List();
            var matches = allContracts
                .Where(c => !c.IsDeleted && c.Name.Contains(contractName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                var availableContracts = allContracts.Where(c => !c.IsDeleted).Select(c => c.Name).ToList();
                var available = availableContracts.Count > 0
                    ? "Available contracts: " + string.Join(", ", availableContracts) + "."
                    : "There are no contracts yet.";
                return SkillResult.Error(
                    $"No contract found matching '{contractName}'. {available} " +
                    "Offer the user only these real contract names — do not invent contracts.");
            }

            if (matches.Count > 1)
            {
                var names = string.Join(", ", matches.Select(c => $"'{c.Name}'"));
                return SkillResult.Error(
                    $"Multiple contracts match '{contractName}': {names}. Please be more specific.");
            }

            contractId = matches[0].Id;
        }

        var entityType = entityTypeValue switch
        {
            "ExternEmp" => EntityTypeEnum.ExternEmp,
            "Customer" => EntityTypeEnum.Customer,
            _ => EntityTypeEnum.Employee
        };

        var resolvedCanton = await ResolveCantonAsync(canton, cancellationToken);

        var result = await _mediator.Send(
            new FillGroupByCriteriaCommand(
                group.Id,
                group.Name,
                resolvedCanton,
                contractId,
                entityType,
                count,
                apply,
                context.UserName),
            cancellationToken);

        if (result.TotalMatchCount == 0)
        {
            return SkillResult.SuccessResult(
                result,
                $"No {entityType} found matching the criteria for group '{group.Name}'.");
        }

        var clientNames = string.Join(", ",
            result.Clients.Select(c => $"{c.FirstName} {c.LastName}".Trim()));

        if (!apply)
        {
            return SkillResult.SuccessResult(
                result,
                $"Preview: {result.Clients.Count} of {result.TotalMatchCount} matching {entityType}(s) " +
                $"would be added to group '{group.Name}': {clientNames}. Nothing was changed yet. " +
                "Ask the user to confirm, then call again with apply=true.");
        }

        var alreadyNote = result.AlreadyMemberCount > 0
            ? $" ({result.AlreadyMemberCount} were already members)"
            : string.Empty;

        return SkillResult.SuccessResult(
            result,
            $"Added {result.AddedCount} {entityType}(s) to group '{group.Name}'{alreadyNote}.");
    }

    private async Task<string?> ResolveCantonAsync(string? canton, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(canton))
        {
            return null;
        }

        var trimmed = canton.Trim();
        var states = (await _mediator.Send(new ListQuery<StateResource>(), cancellationToken)).ToList();

        var byAbbreviation = states.FirstOrDefault(s =>
            string.Equals(s.Abbreviation, trimmed, StringComparison.OrdinalIgnoreCase));
        if (byAbbreviation != null)
        {
            return byAbbreviation.Abbreviation;
        }

        var byName = states.FirstOrDefault(s => s.Name != null &&
            s.Name.ToDictionary().Values.Any(v => string.Equals(v, trimmed, StringComparison.OrdinalIgnoreCase)));

        return byName?.Abbreviation ?? trimmed;
    }
}
