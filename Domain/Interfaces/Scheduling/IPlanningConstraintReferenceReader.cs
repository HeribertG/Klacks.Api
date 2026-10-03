// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Existence checks for what a planning constraint points at: the scope target (group, client, scheduling rule)
/// and the scenario of its AnalyseToken. Used before a constraint is stored or approved, so no rule can be
/// written against a target that does not exist (it would silently apply to nobody).
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface IPlanningConstraintReferenceReader
{
    /// <summary>True when the not deleted group, client or scheduling rule exists; Global has no target and is always true.</summary>
    Task<bool> ScopeTargetExistsAsync(PlanningConstraintScopeType scopeType, Guid? scopeId, CancellationToken cancellationToken = default);

    /// <summary>True when an active (not accepted, rejected or superseded), not deleted scenario carries the token.</summary>
    Task<bool> ActiveScenarioExistsAsync(Guid analyseToken, CancellationToken cancellationToken = default);
}
