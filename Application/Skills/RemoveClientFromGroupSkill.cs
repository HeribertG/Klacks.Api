// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal single-purpose skill: removes an existing client (by name) from a group (by name).
/// Soft-deletes the matching group membership.
/// </summary>
/// <param name="firstName">First name of the client.</param>
/// <param name="lastName">Last name of the client.</param>
/// <param name="groupName">Name of the group to remove the client from.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("remove_client_from_group")]
public class RemoveClientFromGroupSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveClientFromGroupSkill(
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

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        var groups = await _groupRepository.List();
        var group = groups.FirstOrDefault(g => !g.IsDeleted &&
            g.Name.Contains(groupName, StringComparison.OrdinalIgnoreCase));
        if (group == null)
        {
            return SkillResult.Error($"Group '{groupName}' not found.");
        }

        var membership = client!.GroupItems.FirstOrDefault(gi => gi.GroupId == group.Id && !gi.IsDeleted);
        if (membership == null)
        {
            return SkillResult.SuccessResult(
                new { ClientId = client.Id, GroupName = group.Name },
                $"{client.FirstName} {client.Name} is not in group '{group.Name}'.");
        }

        client.GroupItems.Remove(membership);
        client.UpdateTime = DateTime.UtcNow;
        client.CurrentUserUpdated = context.UserName;

        await _clientRepository.Put(client);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { ClientId = client.Id, client.FirstName, LastName = client.Name, GroupName = group.Name },
            $"{client.FirstName} {client.Name} removed from group '{group.Name}'.");
    }
}
