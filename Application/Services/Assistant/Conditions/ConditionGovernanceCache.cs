// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Governance decisions already resolved in one action tick, per scope. A plain dictionary keyed by
/// Guid? cannot express this - Dictionary's key is constrained to notnull - and "no group" is a real,
/// distinct scope here (the installation-wide rule), not an absent one, so it gets its own slot rather
/// than a Guid.Empty sentinel that a real group id could one day collide with. Lives in its own file
/// rather than nested in AgentConditionActionService so that class stays under its size-guard ceiling.
/// </summary>
/// <param name="groupId">Scope key: a group id, or null for the installation-wide rule.</param>
/// <param name="decision">The resolved governance decision for that scope.</param>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionGovernanceCache
{
    private readonly Dictionary<Guid, ProactiveGovernanceDecision> _byGroup = new();
    private ProactiveGovernanceDecision? _installationWide;

    public bool TryGet(Guid? groupId, out ProactiveGovernanceDecision? decision)
    {
        if (groupId is not { } scopedGroupId)
        {
            decision = _installationWide;
            return _installationWide is not null;
        }

        return _byGroup.TryGetValue(scopedGroupId, out decision);
    }

    public void Set(Guid? groupId, ProactiveGovernanceDecision decision)
    {
        if (groupId is { } scopedGroupId)
        {
            _byGroup[scopedGroupId] = decision;
            return;
        }

        _installationWide = decision;
    }
}
