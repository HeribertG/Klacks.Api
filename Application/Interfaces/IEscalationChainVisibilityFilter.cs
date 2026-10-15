// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Decides which escalation chains a caller may see and act on. Visible are: every chain for an administrator; a chain
/// in which the caller holds a Pending or Notified stage (a woken person must always reach the chain they were woken
/// for, even when it has no group or lies outside their group visibility); otherwise only chains whose group lies
/// inside the caller's group visibility. A chain without a group is therefore administrator-or-stage-holder only
/// (fail-closed), and a hidden chain must be answered exactly like a missing one. Chains must be loaded with their stages.
/// </summary>
public interface IEscalationChainVisibilityFilter
{
    /// <param name="chains">Candidate chains, loaded with their stages</param>
    /// <param name="userId">Id of the calling user</param>
    Task<List<EscalationChain>> FilterVisibleAsync(
        IReadOnlyCollection<EscalationChain> chains, string userId, CancellationToken cancellationToken = default);

    /// <summary>The same rule for chains that already ended: everybody who held any stage of it, or cancelled it,
    /// keeps seeing it, because no stage is open any more that would grant the holder path.</summary>
    /// <param name="chains">Ended chains, loaded with their stages</param>
    /// <param name="userId">Id of the calling user</param>
    Task<List<EscalationChain>> FilterVisibleEndedAsync(
        IReadOnlyCollection<EscalationChain> chains, string userId, CancellationToken cancellationToken = default);

    /// <param name="chain">Chain to check, loaded with its stages</param>
    /// <param name="userId">Id of the calling user</param>
    Task<bool> IsVisibleAsync(EscalationChain chain, string userId, CancellationToken cancellationToken = default);
}
