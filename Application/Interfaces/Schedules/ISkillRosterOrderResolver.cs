// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides the roster order a Klacksy skill hands to a wizard start: the saved individual order of the human the
/// skill acts for, or the automatic order (guaranteed hours, decided by the wizard) when the caller is another AI.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface ISkillRosterOrderResolver
{
    /// <summary>
    /// Resolves the employee order a skill call plans with and whether the planner set it by hand.
    /// </summary>
    /// <param name="context">The skill call; tells who triggered it (chat user, external agent, self-reflection plan)</param>
    /// <param name="groupId">Group whose saved order applies</param>
    /// <param name="agentIds">Employees of the run in their incoming order</param>
    Task<(IReadOnlyList<Guid> AgentIds, bool UserDefined)> ResolveAsync(
        SkillExecutionContext context,
        Guid groupId,
        IReadOnlyList<Guid> agentIds,
        CancellationToken cancellationToken);
}
