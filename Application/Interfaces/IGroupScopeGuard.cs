// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Resolves the group scope of the user behind a skill execution so group skills only act on groups
/// the caller is allowed to see. Follows IGroupVisibilityService exactly (fail-closed): admins and
/// installations without groups are unrestricted, every other user is limited to the subtrees of
/// their visibility rows and sees nothing without a row; an execution without a resolvable user
/// sees nothing either. Coupling: IClientVisibilityGuard and IGroupVisibilityGuard read the ambient user and treat a
/// missing one as an unrestricted background job, so the two only agree because SkillExecutorService sets the
/// ExecutionPrincipal from context.UserId for every skill run; a skill reached without that principal and without an
/// HTTP user is fail-closed here but unrestricted there.
/// </summary>
public interface IGroupScopeGuard
{
    Task<GroupScopeAccess> GetAccessAsync(
        SkillExecutionContext context, CancellationToken cancellationToken = default);
}
