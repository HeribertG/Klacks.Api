// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Group-visibility rules for AnalyseScenarios, built on IGroupVisibilityGuard so admins and background callers
/// without a user stay unrestricted. A scenario bound to a group is visible when that group is; a group-less
/// scenario clones and replaces the schedule of every group, so only an unrestricted caller may see or create one.
/// A hidden scenario is answered exactly like a missing one (same KeyNotFoundException text, 404).
/// </summary>
/// <param name="guard">Decides admin status and the caller's visible groups including subgroups</param>
/// <param name="groupId">Group a scenario is bound to or created for; null means the scenario spans all groups</param>
/// <param name="scenario">Scenario loaded by id or token; null when it does not exist</param>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Groups;

public static class ScenarioGroupVisibility
{
    public const string ScenarioNotFoundFormat = "AnalyseScenario with ID {0} not found";

    public const string GrouplessScopeForbiddenMessage =
        "Only unrestricted users may work with scenarios that span all groups";

    public static Task<bool> IsScenarioScopeVisibleAsync(
        this IGroupVisibilityGuard guard, Guid? groupId, CancellationToken cancellationToken = default)
    {
        return groupId is { } id
            ? guard.IsGroupVisibleAsync(id, cancellationToken)
            : guard.IsUnrestrictedAsync(cancellationToken);
    }

    public static async Task<bool> IsScenarioVisibleAsync(
        this IGroupVisibilityGuard guard, AnalyseScenario? scenario, CancellationToken cancellationToken = default)
    {
        return scenario is not null
            && await guard.IsScenarioScopeVisibleAsync(scenario.GroupId, cancellationToken);
    }

    /// <summary>
    /// Returns the scenario when the caller may see it; throws the not-found exception of a missing scenario otherwise.
    /// </summary>
    public static async Task<AnalyseScenario> EnsureScenarioVisibleAsync(
        this IGroupVisibilityGuard guard, AnalyseScenario? scenario, Guid scenarioId, CancellationToken cancellationToken = default)
    {
        if (!await guard.IsScenarioVisibleAsync(scenario, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(ScenarioNotFoundFormat, scenarioId));
        }

        return scenario!;
    }

    /// <summary>
    /// Guards a write that targets a whole scenario scope (create, delete all of a group). A hidden group is
    /// answered like a missing one; a group-less scope needs an unrestricted caller, as the tenant-wide group
    /// commands do.
    /// </summary>
    public static async Task EnsureScenarioScopeWritableAsync(
        this IGroupVisibilityGuard guard, Guid? groupId, CancellationToken cancellationToken = default)
    {
        if (groupId is { } id)
        {
            if (!await guard.IsGroupVisibleAsync(id, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(GroupItemVisibilityMessages.GroupNotFound, id));
            }

            return;
        }

        if (!await guard.IsUnrestrictedAsync(cancellationToken))
        {
            throw new ForbiddenException(GrouplessScopeForbiddenMessage);
        }
    }
}
