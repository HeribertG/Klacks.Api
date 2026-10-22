// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Runs ONE detector through the whole proactive pipeline: ledger upsert, notification, the Detected to
/// Reported transition and the resolve reconciliation. The hourly tick runs every registered detector through
/// it, and an event-driven caller (a completed ERP import) runs a single detector through the very same steps,
/// so both obey the same ledger and dedup rules. Must not be called inside an ambient
/// transaction: the ledger repository opens its own per transition.
/// </summary>
public interface IAgentTriggerDetectorRunner
{
    /// <param name="detector">The detector to run once.</param>
    /// <param name="cancellationToken">Cancels the run between steps.</param>
    Task<AgentTriggerDetectorRunOutcome> RunAsync(IAgentTriggerDetector detector, CancellationToken cancellationToken = default);
}
