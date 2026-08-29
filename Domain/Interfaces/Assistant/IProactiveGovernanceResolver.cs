// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answers "how far may Klacksy go with this finding" for the proactive action branch. The single
/// entry point later stages consult: it folds the stored rule, the fail-safe defaults, the global kill
/// switch and the global autonomy level into one decision, so no caller reimplements that precedence.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IProactiveGovernanceResolver
{
    /// <summary>
    /// The effective governance for one kind. A groupId first looks for that group's scope exception
    /// and falls back to the installation-wide rule; a kind with no stored rule at all resolves to the
    /// defaults, which report and wait. Never returns null.
    /// </summary>
    Task<ProactiveGovernanceDecision> ResolveAsync(
        string triggerKind, Guid? groupId, CancellationToken cancellationToken);

    /// <summary>Effective governance for every governed kind, installation-wide scope.</summary>
    Task<IReadOnlyList<ProactiveGovernanceDecision>> ResolveAllAsync(CancellationToken cancellationToken);

    /// <summary>Whether the global off switch for the proactive action branch is currently set.</summary>
    Task<bool> IsKillSwitchActiveAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The installation-wide autonomy level (0-3) that caps every kind's MaxAction and any delegation,
    /// independent of the kill switch. Falls back to ProactiveGovernanceDefaults.GlobalAutonomyLevel
    /// (0, Hint) when unset or unparseable.
    /// </summary>
    Task<AutonomyLevel> GetGlobalAutonomyLevelAsync(CancellationToken cancellationToken);
}
