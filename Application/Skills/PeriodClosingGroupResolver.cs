// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared helper for the period-closing skills: resolves the optional group scope from a
/// groupId UUID or a groupName (lone-match via GroupResolver; fuzzy only under the Lenient policy
/// of the read-only period skills, Strict for close/reopen) and enforces the caller's group scope. Returns (null, null, null) when no group was supplied and the caller
/// is unrestricted — the caller then operates globally. A group-restricted caller must name a
/// group: an installation-wide answer would include clients outside their scope.
/// </summary>
/// <param name="policy">Strict (default, close/reopen) or Lenient (read-only period skills only).</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

internal static class PeriodClosingGroupResolver
{
    private const string GroupRequiredTemplate =
        "Your group scope is restricted, so a period can only be checked for one of your groups. " +
        "Pass groupId or groupName of a group under: {0}.";

    private const string NoGroupScopeError =
        "Your group scope is restricted and no group is assigned to you, so no period data is visible. " +
        "Ask an administrator to assign you a group scope.";

    public static async Task<(Guid? GroupId, string? GroupName, string? Error)> ResolveAsync(
        string? groupIdRaw,
        string? groupNameRaw,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        SkillExecutionContext context,
        CancellationToken cancellationToken,
        NameMatchPolicy policy = NameMatchPolicy.Strict)
    {
        var scope = await groupScopeGuard.GetAccessAsync(context, cancellationToken);

        if (string.IsNullOrWhiteSpace(groupIdRaw) && string.IsNullOrWhiteSpace(groupNameRaw))
        {
            if (scope.IsUnrestricted)
            {
                return (null, null, null);
            }

            return (null, null, scope.VisibleRootNames.Count == 0
                ? NoGroupScopeError
                : string.Format(GroupRequiredTemplate, string.Join(", ", scope.VisibleRootNames)));
        }

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
        var (resolved, resolveError) = GroupResolver.Resolve(groups, groupNameRaw, policy);
        if (resolveError != null)
        {
            return (null, null, resolveError);
        }

        return (resolved!.Id, resolved.Name, null);
    }
}
