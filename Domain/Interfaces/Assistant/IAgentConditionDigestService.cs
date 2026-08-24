// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Aggregates the condition ledger into one daily inbox message per planner (Etappe 3h). Unlike the
/// detector tick this is not "did the world change" but "what does the world currently look like",
/// so it runs on its own schedule (default 06:30 local, configurable) rather than the trigger tick's
/// hourly cadence, and it dispatches through IAgentTriggerService directly instead of the
/// detector-to-ledger path - there is nothing here for the ledger to record as newly detected.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentConditionDigestService
{
    /// <summary>
    /// Runs the digest if, and only if, the installation's local time has passed the configured time of
    /// day AND today's digest has not already run (checked against a persisted marker so a restart never
    /// re-sends it and two API instances can never both send it - see AgentConditionDigestOutcome). A
    /// server that was offline at the target time catches up on the next call after it comes back, rather
    /// than waiting for the exact minute to line up again.
    /// </summary>
    Task<AgentConditionDigestRunResult> RunIfDueAsync(CancellationToken cancellationToken = default);
}

/// <summary>Why RunIfDueAsync did, or did not, send anything.</summary>
public enum AgentConditionDigestOutcome
{
    /// <summary>The installation's local time has not yet reached the configured time of day today.</summary>
    NotDueYet,

    /// <summary>Today's digest was already sent (by this instance or another one) before this call.</summary>
    AlreadyRanToday,

    /// <summary>Another instance won the compare-and-swap claim for today between this call's checks and its attempt to claim it.</summary>
    LostRace,

    /// <summary>This call claimed today and ran the aggregation, whether or not any planner had a finding to report.</summary>
    Ran
}

/// <summary>Outcome of one RunIfDueAsync call, together with how many planners actually received a message when it ran.</summary>
public sealed record AgentConditionDigestRunResult(AgentConditionDigestOutcome Outcome, int RecipientsNotified);
