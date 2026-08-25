// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence for the per-trigger-kind governance rules that decide how far Klacksy may act on its own.
/// </summary>
/// <remarks>
/// This is the self-committing repository convention (not the stage-only + IUnitOfWork one used by
/// core-domain repositories), matching IAgentConditionRepository in the same feature: the callers are
/// the setting skill and, from Etappe 5, the action dispatcher on the heartbeat, neither of which sits
/// in an HTTP request cycle. Never call a writing method here between a stage-only repository write and
/// its IUnitOfWork.CompleteAsync() - the SaveChangesAsync flushes the whole shared DbContext, including
/// that pending write.
/// </remarks>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentTriggerGovernanceRepository
{
    /// <summary>Every stored rule, installation-wide and group-scoped alike.</summary>
    Task<IReadOnlyList<AgentTriggerGovernance>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The single stored rule for one scope, or null when none was ever written. A null groupId asks
    /// for the installation-wide rule; callers that want the effective values of an unwritten scope use
    /// IProactiveGovernanceResolver instead, which folds in the defaults and the kill switch.
    /// </summary>
    Task<AgentTriggerGovernance?> FindAsync(string triggerKind, Guid? groupId, CancellationToken cancellationToken);

    /// <summary>
    /// Writes the rule for one scope, inserting it when absent. Commits immediately.
    /// </summary>
    Task<AgentTriggerGovernance> UpsertAsync(AgentTriggerGovernance governance, CancellationToken cancellationToken);
}
