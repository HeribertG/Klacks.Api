// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shows the full detail of one group: name, description, validity window, payment interval,
/// holiday-calendar assignment, coordinates, its position in the tree (root-to-node path),
/// number of direct child groups and number of client members. The group is addressed by
/// UUID or by name (staged name resolution with disambiguation instead of guessing).
/// </summary>
/// <param name="groupId">Optional. UUID of the group; takes precedence over groupName.</param>
/// <param name="groupName">Optional. Display name of the group; resolved with fuzzy matching.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_group_details")]
public class GetGroupDetailsSkill : BaseSkillImplementation
{
    private const string PathSeparator = " > ";
    private const string OpenEndedLabel = "open-ended";
    private const string DateFormat = "yyyy-MM-dd";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IMediator _mediator;

    public GetGroupDetailsSkill(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IMediator mediator)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);

        Group? group;
        var groupIdStr = GetParameter<string>(parameters, "groupId");
        if (!string.IsNullOrWhiteSpace(groupIdStr))
        {
            if (!Guid.TryParse(groupIdStr, out var groupId))
            {
                return SkillResult.Error($"Invalid groupId UUID: {groupIdStr}");
            }

            group = await _groupRepository.Get(groupId);
            if (group == null)
            {
                return SkillResult.Error($"Group with ID '{groupId}' not found.");
            }
        }
        else
        {
            var groupName = GetParameter<string>(parameters, "groupName");
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return SkillResult.Error("Provide either groupId or groupName to identify the group.");
            }

            var groups = scope.Filter(await _groupRepository.List());
            var (resolved, resolveError) = GroupResolver.Resolve(groups, groupName);
            if (resolveError != null)
            {
                return SkillResult.Error(resolveError);
            }

            group = resolved!;
        }

        if (!scope.IsInScope(group))
        {
            return SkillResult.Error(scope.BuildOutOfScopeError(group.Name));
        }

        var path = (await _groupRepository.GetPath(group.Id)).ToList();
        var pathDisplay = string.Join(PathSeparator, path.Select(g => g.Name));

        var children = (await _groupRepository.GetChildren(group.Id)).ToList();

        var members = await _mediator.Send(new GetGroupMembersQuery(group.Id), cancellationToken);
        var clientMemberCount = members.Count(m => m.ClientId.HasValue);

        var validUntilDisplay = group.ValidUntil.HasValue
            ? group.ValidUntil.Value.ToString(DateFormat)
            : OpenEndedLabel;

        return SkillResult.SuccessResult(
            new
            {
                GroupId = group.Id,
                group.Name,
                group.Description,
                ValidFrom = group.ValidFrom.ToString(DateFormat),
                ValidUntil = validUntilDisplay,
                PaymentInterval = group.PaymentInterval.ToString(),
                group.CalendarSelectionId,
                group.Latitude,
                group.Longitude,
                ParentId = group.Parent,
                Path = pathDisplay,
                DirectChildCount = children.Count,
                ClientMemberCount = clientMemberCount
            },
            $"Group '{group.Name}' ({pathDisplay}): valid {group.ValidFrom.ToString(DateFormat)} to {validUntilDisplay}, " +
            $"payment interval {group.PaymentInterval}, {children.Count} direct child group(s), " +
            $"{clientMemberCount} client member(s). Use list_group_members for the member list.");
    }
}
