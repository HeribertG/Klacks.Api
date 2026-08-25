// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether right now is an unfavourable moment to remediate an open AgentCondition (Etappe 4f
/// of the Klacksy-proactive plan): a running optimizer job, an active ERP import, or a sealed/locked
/// target entity all mean the condition should be left alone this tick. A quiet result does not change
/// the ledger and does not count as an attempt - the caller (the Etappe 5 action dispatcher) simply
/// skips the condition without touching AttemptCount or LastAttemptAtUtc, so the next tick tries again.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IQuietWindowService
{
    /// <summary>
    /// Whether the given condition should be skipped this tick because now is an unfavourable moment
    /// to act on it.
    /// </summary>
    /// <param name="condition">The open ledger row a remediation is being considered for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> IsQuietForAsync(AgentCondition condition, CancellationToken cancellationToken = default);
}
