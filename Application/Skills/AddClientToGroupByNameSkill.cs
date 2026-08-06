// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal single-purpose skill: adds an existing client (by name) to a group (by name).
/// </summary>
/// <param name="firstName">First name of the client.</param>
/// <param name="lastName">Last name of the client.</param>
/// <param name="groupName">Name of the group to add the client to.</param>
/// <param name="idNumber">Optional visible client number to disambiguate duplicate names.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

using Klacks.Api.Application.DTOs.Associations;

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class AddClientToGroupByNameSkill : BaseSkillImplementation
{
    private const string SkillName = "add_client_to_group_by_name";

    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly ICompanyClock _companyClock;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;

    public AddClientToGroupByNameSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        ICompanyClock companyClock,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _companyClock = companyClock;
        _selfApi = selfApi;
        _routes = routes;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var groupName = GetRequiredString(parameters, "groupName");

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (validFrom, invalidDate) = SkillDateParser.ParseOptionalUtcDate(
            GetParameter<string>(parameters, "validFrom"), today);
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessage);
        }

        var idNumber = GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName);
        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, idNumber, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        if (context.SelectedEntityIds is { Count: > 1 } selection && selection.Contains(client!.Id))
        {
            return SkillResult.Error(
                $"{client.FirstName} {client.Name} is one of the {selection.Count} people currently selected in " +
                "the list. To add the selected people to a group reliably — this also handles duplicate names " +
                "correctly — call add_selected_clients_to_group with apply=true instead of adding them one by " +
                "one. Only add this single person by name if the user explicitly asked for just them.");
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var groups = scope.Filter(await _groupRepository.List());
        var (group, groupError) = GroupResolver.Resolve(groups, groupName);
        if (group == null)
        {
            return SkillResult.Error(groupError!);
        }

        if (client!.GroupItems.Any(gi => gi.GroupId == group.Id && !gi.IsDeleted))
        {
            return SkillResult.SuccessResult(
                new { ClientId = client.Id, GroupName = group.Name },
                $"{client.FirstName} {client.Name} is already in group '{group.Name}'.");
        }

        if (validFrom is null)
        {
            return SkillResult.Error(
                SkillDateParser.MissingStartDateMessage(
                    $"add {client.FirstName} {client.Name} to group '{group.Name}'"));
        }

        var resource = new GroupItemResource
        {
            ClientId = client.Id,
            GroupId = group.Id,
            ValidFrom = validFrom.Value
        };

        var result = await _selfApi.PostAsync<GroupItemResource>(
            _routes.Resolve(typeof(GroupItemResource)), resource, context, SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        return SkillResult.SuccessResult(
            new { ClientId = client.Id, client.FirstName, LastName = client.Name, GroupName = group.Name },
            $"{client.FirstName} {client.Name} added to group '{group.Name}'.");
    }
}
