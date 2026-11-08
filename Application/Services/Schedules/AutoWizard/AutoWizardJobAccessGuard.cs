// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;

namespace Klacks.Api.Application.Services.Schedules.AutoWizard;

/// <summary>
/// Decides job access from the caller's visibility: admins, callers without a user and installations without groups
/// pass; a restricted caller passes only for an AutoWizard job whose recorded planning group and every recorded employee
/// are visible to them. The job's status and events name the employees it planned and the members of its group (with its
/// subgroups), so they then reveal nobody the caller cannot already see. Jobs of the other wizard families are never
/// recorded and therefore stay with unrestricted callers.
/// </summary>
/// <param name="groupVisibilityGuard">The caller's group visibility</param>
/// <param name="clientVisibilityGuard">The caller's visibility of employees</param>
/// <param name="jobScopes">Planning group and employees of each started AutoWizard job</param>
public sealed class AutoWizardJobAccessGuard : IAutoWizardJobAccessGuard
{
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly AutoWizardJobScopes _jobScopes;

    public AutoWizardJobAccessGuard(
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard,
        AutoWizardJobScopes jobScopes)
    {
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
        _jobScopes = jobScopes;
    }

    public async Task<bool> CanAccessAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (await _groupVisibilityGuard.IsUnrestrictedAsync(cancellationToken))
        {
            return true;
        }

        var scope = _jobScopes.Find(jobId);
        return scope?.GroupId is { } groupId
            && await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken)
            && await _clientVisibilityGuard.AreAllVisibleAsync(scope.AgentIds, cancellationToken);
    }
}
