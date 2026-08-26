// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What one run of the action dispatcher did, per outcome rather than as a single number. Every way a
/// condition can be passed over has its own counter on purpose: "no silent caps" is a requirement of
/// this stage, and a tick that reports only its executions cannot tell an idle installation apart from
/// one where the budget, the quiet window or an unbindable payload stopped everything.
/// </summary>
/// <param name="Considered">Candidate rows examined across all remediable kinds.</param>
/// <param name="Executed">Remediations that ran and reported success.</param>
/// <param name="Failed">Remediations that ran and reported failure or could not be given an identity; the row stays Prepared and is retried.</param>
/// <param name="Escalated">Rows moved to Escalated after MaxAttemptsBeforeEscalation attempts.</param>
/// <param name="SkippedCascade">Rows a Klacksy execution is suspected to have caused, which are never auto-handled.</param>
/// <param name="SkippedQuiet">Rows inside a quiet window; deliberately NOT counted as an attempt.</param>
/// <param name="SkippedUnbindable">Rows whose payload cannot produce the remediation's required arguments.</param>
/// <param name="SkippedNoOwner">Rows whose kind has no responsible owner, so no identity could be borrowed.</param>
/// <param name="SkippedClaimLost">Claims another instance won, or a compare-and-swap that reported a false negative.</param>
/// <param name="LeftForBudget">Rows left open because the daily budget or the circuit breaker was reached.</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record AgentConditionActionTickResult(
    int Considered,
    int Executed,
    int Failed,
    int Escalated,
    int SkippedCascade,
    int SkippedQuiet,
    int SkippedUnbindable,
    int SkippedNoOwner,
    int SkippedClaimLost,
    int LeftForBudget);
