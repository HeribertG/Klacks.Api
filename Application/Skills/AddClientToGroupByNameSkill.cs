// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal single-purpose skill: adds an existing client (by name) to a group (by name).
/// </summary>
/// <param name="firstName">First name of the client.</param>
/// <param name="lastName">Last name of the client.</param>
/// <param name="groupName">Name of the group to add the client to.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_client_to_group_by_name")]
public class AddClientToGroupByNameSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddClientToGroupByNameSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var groupName = GetRequiredString(parameters, "groupName");

        var (validFrom, invalidDate) = SkillDateParser.ParseOptionalUtcDate(
            GetParameter<string>(parameters, "validFrom"));
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessage);
        }

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, cancellationToken);
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

        var groups = await _groupRepository.List();
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

        var now = DateTime.UtcNow;
        client.GroupItems.Add(new GroupItem
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            GroupId = group.Id,
            ValidFrom = validFrom.Value,
            CreateTime = now,
            CurrentUserCreated = context.UserName
        });
        client.UpdateTime = now;
        client.CurrentUserUpdated = context.UserName;

        await _clientRepository.Put(client);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { ClientId = client.Id, client.FirstName, LastName = client.Name, GroupName = group.Name },
            $"{client.FirstName} {client.Name} added to group '{group.Name}'.");
    }
}
