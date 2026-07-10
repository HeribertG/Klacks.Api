// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for the period-closing skills: resolves the optional group scope from a
/// groupId UUID or a fuzzy groupName (lone-match via GroupResolver) and enforces the
/// caller's group scope. Returns (null, null, null) when no group was supplied — the
/// caller then operates globally.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

internal static class PeriodClosingGroupResolver
{
    public static async Task<(Guid? GroupId, string? GroupName, string? Error)> ResolveAsync(
        string? groupIdRaw,
        string? groupNameRaw,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        SkillExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(groupIdRaw) && string.IsNullOrWhiteSpace(groupNameRaw))
        {
            return (null, null, null);
        }

        var scope = await groupScopeGuard.GetAccessAsync(context, cancellationToken);

        if (!string.IsNullOrWhiteSpace(groupIdRaw))
        {
            if (!Guid.TryParse(groupIdRaw, out var groupId))
            {
                return (null, null, $"Invalid groupId UUID: {groupIdRaw}");
            }

            var group = await groupRepository.Get(groupId);
            if (group == null)
            {
                return (null, null, $"Group with ID '{groupId}' not found.");
            }

            if (!scope.IsInScope(group))
            {
                return (null, null, scope.BuildOutOfScopeError(group.Name));
            }

            return (group.Id, group.Name, null);
        }

        var groups = scope.Filter(await groupRepository.List());
        var (resolved, resolveError) = GroupResolver.Resolve(groups, groupNameRaw);
        if (resolveError != null)
        {
            return (null, null, resolveError);
        }

        return (resolved!.Id, resolved.Name, null);
    }
}
