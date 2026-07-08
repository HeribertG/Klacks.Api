// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Resolves the group scope of the user behind a skill execution so group-mutating skills only
/// act on groups the caller is allowed to see. Executions without a resolvable user
/// (system/background contexts) and users without a configured scope are unrestricted.
/// </summary>
public interface IGroupScopeGuard
{
    Task<GroupScopeAccess> GetAccessAsync(
        SkillExecutionContext context, CancellationToken cancellationToken = default);
}
