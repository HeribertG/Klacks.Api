// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The hard limits of the autonomous action branch (Etappe 5b). These are deliberately code constants
/// and not settings: agent_trigger_governance already exposes the per-kind budget an administrator may
/// tune, and everything here is the backstop that stays in force no matter how that row is configured.
/// The plan's original "at most 20 % of the target period's shifts" rule is NOT among them - it has no
/// defined denominator and is semantically empty for a remediation that creates a template rather than
/// touching existing shifts; <see cref="MaxExecutionsPerKindPerTick"/> replaces it with a number that
/// can actually be checked.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AgentConditionActionDefaults
{
    /// <summary>
    /// Attempts a single condition gets before it is escalated as ineffective. Counted on the CLAIM,
    /// never on the outcome: a run that crashes between claim and result would otherwise never raise
    /// the counter and would be retried forever.
    /// </summary>
    public const int MaxAttemptsBeforeEscalation = 3;

    /// <summary>
    /// Minutes after which a Prepared row whose claim produced no outcome is considered abandoned and
    /// may be claimed again. Must stay below <see cref="ProactiveHeartbeat.ScanIntervalMinutes"/>, or a
    /// crashed claim would survive every following tick and the row would be stuck in Prepared forever.
    /// </summary>
    public const int StaleClaimMinutes = 30;

    /// <summary>
    /// Absolute number of executions one trigger kind may reach in one tick, independent of what
    /// governance configured. A misconfigured DailyActionBudget cannot widen it.
    /// </summary>
    public const int MaxExecutionsPerKindPerTick = 5;

    /// <summary>
    /// Rows one kind's candidate query reads per tick. Higher than
    /// <see cref="MaxExecutionsPerKindPerTick"/> on purpose: the cap applies to EXECUTIONS, while the
    /// candidate list is also what the escalation and cascade-marking passes walk.
    /// </summary>
    public const int CandidateQueryCap = 200;

    /// <summary>
    /// How far back the cascade guard looks for a Klacksy execution on the same entity. One scan
    /// interval, so it covers exactly "the first tick after an execution" and no more.
    /// </summary>
    public const int CascadeWindowMinutes = ProactiveHeartbeat.ScanIntervalMinutes;

    /// <summary>
    /// Marker every audit event that CONSUMES action budget starts with. The budget and the circuit
    /// breaker count these rows, which is what makes the counting multi-instance correct: the claim's
    /// event is written in the same database transaction as the compare-and-swap, so a claim can never
    /// exist without its budget row - not even when the swap reports a false negative after committing.
    /// Deliberately distinct from <see cref="ActionOutcomeDetailPrefix"/>, so an outcome event can
    /// never be counted a second time against the same budget.
    /// </summary>
    public const string ActionClaimDetailPrefix = "klacksy-claim:";

    /// <summary>Marker for audit events that REPORT what an action did; never counted against budget.</summary>
    public const string ActionOutcomeDetailPrefix = "klacksy-action:";

    /// <summary>Topic of the durable notes the mandatory post-action report is stashed under.</summary>
    public const string ReportNoteTopic = "proactive-action";
}
