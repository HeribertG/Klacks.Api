// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_client_to_group")]
public class AddClientToGroupSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddClientToGroupSkill(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientIdStr = GetRequiredString(parameters, "clientId");
        var groupIdStr = GetRequiredString(parameters, "groupId");
        var validFromStr = GetParameter<string>(parameters, "validFrom");
        var validUntilStr = GetParameter<string>(parameters, "validUntil");

        if (!Guid.TryParse(clientIdStr, out var clientId))
        {
            return SkillResult.Error($"Invalid client ID format: {clientIdStr}");
        }

        if (!Guid.TryParse(groupIdStr, out var groupId))
        {
            return SkillResult.Error($"Invalid group ID format: {groupIdStr}");
        }

        var clientExists = await _clientRepository.Exists(clientId);
        if (!clientExists)
        {
            return SkillResult.Error($"Client with ID {clientId} not found.");
        }

        var group = await _groupRepository.Get(groupId);
        if (group == null)
        {
            return SkillResult.Error($"Group with ID {groupId} not found.");
        }

        var existingMembership = await _groupItemRepository.GetByClientAndGroup(clientId, groupId);
        if (existingMembership != null && !existingMembership.IsDeleted)
        {
            return SkillResult.Error($"Client is already a member of group '{group.Name}'.");
        }

        DateTime? validFrom = null;
        if (!string.IsNullOrEmpty(validFromStr) && DateTime.TryParse(validFromStr, out var parsedFrom))
        {
            validFrom = DateTime.SpecifyKind(parsedFrom, DateTimeKind.Utc);
        }

        DateTime? validUntil = null;
        if (!string.IsNullOrEmpty(validUntilStr) && DateTime.TryParse(validUntilStr, out var parsedUntil))
        {
            validUntil = DateTime.SpecifyKind(parsedUntil, DateTimeKind.Utc);
        }

        var groupItem = new GroupItem
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            GroupId = groupId,
            ValidFrom = validFrom ?? DateTime.UtcNow,
            ValidUntil = validUntil,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        await _groupItemRepository.Add(groupItem);
        await _unitOfWork.CompleteAsync();

        var resultData = new
        {
            GroupItemId = groupItem.Id,
            ClientId = clientId,
            GroupId = groupId,
            GroupName = group.Name,
            ValidFrom = groupItem.ValidFrom,
            ValidUntil = groupItem.ValidUntil
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Client successfully added to group '{group.Name}'.");
    }
}
