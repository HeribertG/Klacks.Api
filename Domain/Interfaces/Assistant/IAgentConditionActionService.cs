// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The action branch of the proactive heartbeat (Etappe 5b): the one place where Klacksy carries a
/// remediation out on its own instead of reporting it. Runs as a sibling step of the detector scan, on
/// the ledger rows the detectors left behind rather than on this tick's findings - which is what lets a
/// finding be reported in one tick and acted on in a later one, and what makes the whole branch
/// restartable after a crash.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentConditionActionService
{
    /// <summary>
    /// One pass over every remediable trigger kind. Never throws for a single kind's failure - a kind
    /// whose governance, ledger or remediation fails is logged and the remaining kinds still run.
    /// </summary>
    Task<AgentConditionActionTickResult> RunAsync(CancellationToken cancellationToken = default);
}
