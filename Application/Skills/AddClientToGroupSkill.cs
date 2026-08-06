// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Adds an existing client to a group by their UUIDs, writing the membership inside a verified
/// transaction and rejecting a client that is already a member. Requires an explicit start date
/// (validFrom); it asks the user for one instead of silently defaulting to today.
/// </summary>
/// <param name="clientId">UUID of the client to add to the group.</param>
/// <param name="groupId">UUID of the target group.</param>
/// <param name="validFrom">Membership start date (the plannability boundary in the schedule); required.</param>
/// <param name="validUntil">Optional membership end date; open-ended when omitted.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

using Klacks.Api.Application.DTOs.Associations;

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class AddClientToGroupSkill : BaseSkillImplementation
{
    private const string SkillName = "add_client_to_group";

    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;
    private readonly ICompanyClock _companyClock;

    public AddClientToGroupSkill(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IGroupItemRepository groupItemRepository,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes,
        ICompanyClock companyClock)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _groupItemRepository = groupItemRepository;
        _selfApi = selfApi;
        _routes = routes;
        _companyClock = companyClock;
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

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        if (!scope.IsInScope(group))
        {
            return SkillResult.Error(scope.BuildOutOfScopeError(group.Name));
        }

        var existingMembership = await _groupItemRepository.GetByClientAndGroup(clientId, groupId);
        if (existingMembership != null && !existingMembership.IsDeleted)
        {
            return SkillResult.Error($"Client is already a member of group '{group.Name}'.");
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (validFrom, invalidDate) = SkillDateParser.ParseOptionalUtcDate(validFromStr, today);
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessage);
        }

        if (validFrom is null)
        {
            return SkillResult.Error(
                SkillDateParser.MissingStartDateMessage(
                    $"add this client to group '{group.Name}'"));
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
            ValidFrom = validFrom.Value,
            ValidUntil = validUntil,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        var resource = new GroupItemResource
        {
            ClientId = clientId,
            GroupId = groupId,
            ValidFrom = groupItem.ValidFrom,
            ValidUntil = groupItem.ValidUntil
        };

        var result = await _selfApi.PostAsync<GroupItemResource>(
            _routes.Resolve(typeof(GroupItemResource)), resource, context, SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        var resultData = new
        {
            GroupItemId = groupItem.Id,
            ClientId = clientId,
            GroupId = groupId,
            GroupName = group.Name,
            ValidFrom = groupItem.ValidFrom,
            ValidUntil = groupItem.ValidUntil,
            Verified = true
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Client successfully added to group '{group.Name}'");
    }
}
