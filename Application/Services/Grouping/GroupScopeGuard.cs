// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default group scope guard for skill executions. It holds no visibility rule of its own: the scope comes from
/// IGroupVisibilityService for the user named in the execution context, so Klacksy and the REST/UI paths share one
/// fail-closed truth. Unrestricted for admins and for installations without groups; restricted to the subtrees of
/// the user's visible root groups otherwise, and empty when the user has no visibility row, the context carries no
/// user or the user is unknown.
/// </summary>
/// <param name="groupVisibilityService">Resolves the visibility scope of the executing user</param>
/// <param name="groupRepository">Provides the root groups to translate visible root ids into names</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Grouping;

public class GroupScopeGuard : IGroupScopeGuard
{
    private readonly IGroupVisibilityService _groupVisibilityService;
    private readonly IGroupRepository _groupRepository;

    public GroupScopeGuard(IGroupVisibilityService groupVisibilityService, IGroupRepository groupRepository)
    {
        _groupVisibilityService = groupVisibilityService;
        _groupRepository = groupRepository;
    }

    public async Task<GroupScopeAccess> GetAccessAsync(
        SkillExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (context.UserId == Guid.Empty)
        {
            return GroupScopeAccess.Restricted([], []);
        }

        var scope = await _groupVisibilityService.GetVisibilityScopeForUserAsync(
            context.UserId.ToString(), cancellationToken);
        if (scope.IsUnrestricted)
        {
            return GroupScopeAccess.Unrestricted();
        }

        if (scope.VisibleRootIds.Count == 0)
        {
            return GroupScopeAccess.Restricted([], []);
        }

        var visibleRootIds = scope.VisibleRootIds.ToHashSet();
        var roots = await _groupRepository.GetRoots();
        var visibleRootNames = roots
            .Where(r => visibleRootIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToList();

        return GroupScopeAccess.Restricted(visibleRootIds, visibleRootNames);
    }
}
