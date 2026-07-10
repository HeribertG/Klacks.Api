// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the client members of a group (the people shown in the group's member card),
/// including each membership's validity window. Shift assignments to the group are not
/// listed here — this skill answers "who is in this group". The group is addressed by
/// UUID or by name (staged name resolution with disambiguation instead of guessing).
/// </summary>
/// <param name="groupId">Optional. UUID of the group; takes precedence over groupName.</param>
/// <param name="groupName">Optional. Display name of the group; resolved with fuzzy matching.</param>
/// <param name="limit">Optional. Maximum number of members to return (default 50).</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_group_members")]
public class ListGroupMembersSkill : BaseSkillImplementation
{
    private const int DefaultLimit = 50;
    private const string OpenEndedLabel = "open-ended";
    private const string DateFormat = "yyyy-MM-dd";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IMediator _mediator;

    public ListGroupMembersSkill(
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

        var limit = GetParameter<int?>(parameters, "limit") ?? DefaultLimit;
        if (limit < 1)
        {
            limit = DefaultLimit;
        }

        var members = await _mediator.Send(new GetGroupMembersQuery(group.Id), cancellationToken);

        var clientMembers = members
            .Where(m => m.ClientId.HasValue && m.Client != null)
            .OrderBy(m => m.Client!.Name)
            .ThenBy(m => m.Client!.FirstName)
            .ToList();

        var listed = clientMembers
            .Take(limit)
            .Select(m => new
            {
                ClientId = m.ClientId!.Value,
                m.Client!.FirstName,
                LastName = m.Client.Name,
                m.Client.IdNumber,
                ValidFrom = m.ValidFrom?.ToString(DateFormat),
                ValidUntil = m.ValidUntil.HasValue ? m.ValidUntil.Value.ToString(DateFormat) : OpenEndedLabel
            })
            .ToList();

        var truncatedNote = clientMembers.Count > limit
            ? $" Showing the first {limit} of {clientMembers.Count} members — raise the limit parameter for more."
            : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                GroupId = group.Id,
                GroupName = group.Name,
                TotalClientMembers = clientMembers.Count,
                Members = listed
            },
            $"Group '{group.Name}' has {clientMembers.Count} client member(s).{truncatedNote}");
    }
}
