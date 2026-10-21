// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Gate (0) of the action dispatcher: the effective MaxAction of one candidate row. Resolves the per-scope
/// governance once per group and kind run (through the run's own cache) and folds it with the row's
/// Etappe-4e delegation, the global autonomy level and the code-only remediation registry.
/// </summary>
/// <param name="governanceResolver">Per-kind, per-scope MaxAction and budget values (Etappe 4a).</param>
/// <param name="registry">Code-only map from kind to remediation; absence caps the kind at Hint.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionActionGovernance
{
    private readonly IProactiveGovernanceResolver _governanceResolver;
    private readonly IConditionRemediationRegistry _registry;

    public ConditionActionGovernance(
        IProactiveGovernanceResolver governanceResolver, IConditionRemediationRegistry registry)
    {
        _governanceResolver = governanceResolver;
        _registry = registry;
    }

    public async Task<ProactiveGovernanceDecision> ResolveAsync(
        ConditionGovernanceCache cache,
        AgentCondition condition,
        CancellationToken cancellationToken)
    {
        if (cache.TryGet(condition.GroupId, out var cached))
        {
            return cached!;
        }

        var decision = await _governanceResolver.ResolveAsync(
            condition.TriggerKind, condition.GroupId, cancellationToken);
        cache.Set(condition.GroupId, decision);

        return decision;
    }

    /// <summary>
    /// Governance folded with the Etappe-4e delegation, re-capped by the global autonomy level and then
    /// capped by the remediation registry. Precedence is not negotiable in three places: the global kill
    /// switch and a disabled kind pin the result at Hint BEFORE the delegation is looked at, because a
    /// human's earlier "you handle this one" grant must never survive the emergency stop; the global
    /// autonomy level caps the delegation too (Owner decision 2026-08-28); and the registry cap applies
    /// LAST, so no delegation can steer a kind that has no remediation past Hint.
    /// </summary>
    public ProactiveMaxAction EffectiveMaxActionFor(ProactiveGovernanceDecision governance, AgentCondition condition)
    {
        if (governance.KillSwitchActive || !governance.Enabled)
        {
            return ProactiveMaxAction.Hint;
        }

        var requested = condition.DelegatedMaxAction is { } delegated && delegated > governance.EffectiveMaxAction
            ? delegated
            : governance.EffectiveMaxAction;

        var levelCapped = requested < governance.GlobalAutonomyCap ? requested : governance.GlobalAutonomyCap;

        return _registry.TryGetEffectiveMaxAction(condition.TriggerKind, levelCapped);
    }
}
