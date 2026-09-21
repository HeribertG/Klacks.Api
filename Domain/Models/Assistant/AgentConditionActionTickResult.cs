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
/// <param name="SkippedNoApprover">Claimed (Prepared) rows that carry no approval stamp, so there is no identity to resume them under; they are left to age into escalation.</param>
/// <param name="SkippedClaimLost">Claims another instance won, or a compare-and-swap that reported a false negative.</param>
/// <param name="LeftForBudget">Rows left open because the daily budget or the circuit breaker was reached.</param>
/// <param name="ApprovalsRequested">Approval chains this tick opened; the row stays Reported until somebody acknowledges.</param>
/// <param name="AwaitingApproval">Rows passed over because a chain is already Running for them or ended earlier on the same company day.</param>
/// <param name="ApprovalsUnavailable">Rows for which no chain could be opened - no eligible approver, or the start was declined - and which therefore stay unhandled (fail closed).</param>
/// <param name="ApprovalsWithdrawn">Approvals the tick withdrew without executing: the stale-claim window had passed or the approver no longer qualified.</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record AgentConditionActionTickResult(
    int Considered,
    int Executed,
    int Failed,
    int Escalated,
    int SkippedCascade,
    int SkippedQuiet,
    int SkippedUnbindable,
    int SkippedNoApprover,
    int SkippedClaimLost,
    int LeftForBudget,
    int ApprovalsRequested = 0,
    int AwaitingApproval = 0,
    int ApprovalsUnavailable = 0,
    int ApprovalsWithdrawn = 0);
