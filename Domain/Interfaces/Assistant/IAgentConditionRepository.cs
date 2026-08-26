// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence for the condition ledger (agent_conditions) and its append-only audit history
/// (agent_condition_events). Every status change goes through <see cref="TryTransitionAsync"/>, a
/// conditional UPDATE ... WHERE status = expected, so two API instances scanning the same tick can never
/// both advance the same row.
/// </summary>
/// <remarks>
/// This is the self-committing repository convention (not the stage-only + IUnitOfWork one used by
/// core-domain repositories): the callers are the proactive tick and later the action dispatcher, both
/// running outside the HTTP request cycle, and each ledger step must persist on its own so a crashed
/// tick cannot lose a transition that already happened in the outside world. Never call this repository
/// between a stage-only repository write and its IUnitOfWork.CompleteAsync() - the SaveChangesAsync here
/// flushes the whole shared DbContext, including that pending write.
/// </remarks>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentConditionRepository
{
    /// <summary>
    /// Returns the single open (non-terminal) row carrying this fingerprint, or null. Deliberately not
    /// filtered by TriggerKind: the partial unique index is on Fingerprint alone, so a kind filter would
    /// hide a cross-kind collision from the lookup while the index still rejected the insert, leaving the
    /// caller in an unresolvable retry loop.
    /// </summary>
    Task<AgentCondition?> FindOpenByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// The row with this id whatever its status, or null. Unlike <see cref="FindOpenByFingerprintAsync"/>
    /// this deliberately returns terminal rows too: a caller that wants to move a row along needs to see
    /// the status it is actually in before it can pick a compare-and-swap source, and "already terminal"
    /// is an answer it must be able to distinguish from "gone".
    /// </summary>
    Task<AgentCondition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The row whose remediation is this AnalyseScenario, or null. Identity by scenario id rather than
    /// by the scenario's CreatedByUser string: only a row Klacksy prepared ever carries a ScenarioId, so
    /// a hit IS a Klacksy scenario and a human-authored one can never be mistaken for one. Terminal rows
    /// are returned too, so a caller can tell "already rejected" from "never was Klacksy's".
    /// Newest detection first, which only matters if a scenario were ever linked twice - it is written
    /// exactly once, in the transition to Prepared. Indexed already, without a migration of its own:
    /// AgentConditionConfiguration declares ScenarioId as a foreign key to AnalyseScenario, and EF backs
    /// every foreign key with an index (ix_agent_conditions_scenario_id).
    /// </summary>
    Task<AgentCondition?> FindByScenarioIdAsync(Guid scenarioId, CancellationToken cancellationToken = default);

    /// <summary>All open (non-terminal) rows of one detector kind, oldest detection first.</summary>
    Task<List<AgentCondition>> GetOpenByKindAsync(string triggerKind, CancellationToken cancellationToken = default);

    /// <summary>
    /// The AgentConditionPlannerRelevantStatuses.Values rows a planner in this scope may see, up to
    /// <paramref name="take"/>, sorted severity (High before Medium before Low) then
    /// <see cref="AgentCondition.DetectedAtUtc"/> ascending (oldest first) within the same severity.
    /// Deliberately does NOT exclude Escalated: it is a terminal status only for the partial unique index (a
    /// re-arm can open a fresh Detected row for the same fingerprint alongside it), so an Escalated row and a
    /// newer open row of the same underlying condition can legitimately both be in the result at once - that
    /// is the correct picture ("escalated after N attempts, and re-detected since"), not a duplicate to be
    /// collapsed. Scope shape mirrors <see cref="GetTopForContextAsync"/>: same isUnrestricted/visibleRootIds
    /// contract, same kind-dependent GroupId-null rule, same root-comparison against the group's Nested
    /// Set root (not a flattened subtree list) via the GroupId-to-Group join.
    /// </summary>
    /// <param name="isUnrestricted">True for an admin: every row is returned regardless of GroupId.</param>
    /// <param name="visibleRootIds">Ignored when <paramref name="isUnrestricted"/> is true. Otherwise a row is
    /// included when its group's Nested Set root is in this set, or its GroupId is null AND its TriggerKind is
    /// not one of AgentTriggerGroupScopedKinds.Values - for those group-borne kinds a null GroupId means the
    /// group was not determined, so the row stays with Admins instead of reaching every planner. An empty set
    /// fails closed to those ungated rows only - the same semantics AgentConditionVisibilityScope.Restricted
    /// with an empty set already carries for a planner with no GroupVisibility row.</param>
    /// <param name="take">Row cap applied after sorting, so the most urgent rows are the ones kept.</param>
    Task<List<AgentCondition>> GetOpenForScopeAsync(
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Total count of AgentConditionPlannerRelevantStatuses.Values rows a planner in this scope may see,
    /// ignoring <see cref="GetOpenForScopeAsync"/>'s take cap - so a caller can report "showing N of M" instead
    /// of silently truncating. Same scope contract as <see cref="GetOpenForScopeAsync"/>.
    /// </summary>
    Task<int> CountOpenForScopeAsync(
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The single planner-relevant row with this id, under the same scope contract as
    /// <see cref="GetOpenForScopeAsync"/>, or null when it does not exist, is not currently
    /// AgentConditionPlannerRelevantStatuses.Values, or falls outside the caller's scope. Etappe 4e
    /// delegation uses this so it can answer "not found" rather than "forbidden" for a condition outside
    /// the delegating user's own visibility - matching how the scoped list queries already hide
    /// out-of-scope rows instead of revealing that they exist.
    /// </summary>
    Task<AgentCondition?> GetOpenForScopeByIdAsync(
        Guid id,
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a freshly detected condition together with its first audit event in one SaveChangesAsync,
    /// so a ledger row can never exist without the event that opened it. Returns null - not an exception -
    /// when the partial unique index on Fingerprint rejected the insert because another instance opened a
    /// row for the same fingerprint first; the caller is expected to re-read and treat it as known.
    /// </summary>
    Task<AgentCondition?> InsertAsync(AgentCondition condition, AgentConditionEvent detectionEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically moves a row from <paramref name="fromStatus"/> to <paramref name="toStatus"/> and, in the
    /// same database transaction, appends <paramref name="auditEvent"/>. Returns true only when this caller
    /// won the compare-and-swap. The transaction matters because the event rows are not just a log: the
    /// Etappe 5 action budget and circuit breaker are counted from them, so a transition without its event
    /// would silently not count against a safety limit. Because the method opens that transaction itself,
    /// it must not be called from inside IUnitOfWork.ExecuteInTransactionAsync or any other ambient
    /// transaction - EF refuses to nest one, and the call fails loudly rather than degrading.
    /// </summary>
    /// <remarks>
    /// False means "this caller did not win", which is ALMOST always "another instance moved the row first,
    /// nothing was written" - but not exclusively, so a caller must not read it as a guarantee that nothing
    /// happened. It is also false for an unknown or soft-deleted id, and, rarely, after the connection drops
    /// between a successful server-side commit and its acknowledgement: EnableRetryOnFailure (Program.cs)
    /// gives this context a retrying execution strategy, so the whole lambda is replayed, finds the row
    /// already past <paramref name="fromStatus"/>, and reports false although the first attempt did persist
    /// both the transition and its event. Etappe 5 must therefore treat a false as "skip this row for now",
    /// never as "this row's budget was not consumed".
    /// </remarks>
    Task<bool> TryTransitionAsync(
        Guid id,
        AgentConditionStatus fromStatus,
        AgentConditionStatus toStatus,
        AgentConditionTransitionFields? fields,
        AgentConditionEvent auditEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes over a Prepared row whose remediation attempt produced no outcome, and appends
    /// <paramref name="auditEvent"/> in the same database transaction. The compare-and-swap is on
    /// LastAttemptAtUtc rather than on Status, because the state machine has no Prepared-to-Prepared
    /// transition and needs none: the row is already where the execution stage wants it, what has to be
    /// claimed atomically is the RIGHT TO RETRY. Only a row whose LastAttemptAtUtc lies strictly before
    /// <paramref name="staleBeforeUtc"/> is taken, so two instances can never both resume the same row,
    /// and a claim that is still running is left alone. AttemptCount is raised here as well - a crash
    /// loop that only counted successful outcomes would retry forever without ever escalating.
    ///
    /// A row with a NULL LastAttemptAtUtc is deliberately NOT eligible: nothing this service wrote can
    /// produce one, so it would have to come from a Prepared transition made elsewhere (the scenario
    /// preparation path), which this must not hijack.
    /// </summary>
    /// <param name="id">The Prepared row to resume.</param>
    /// <param name="staleBeforeUtc">Cut-off; a claim older than this counts as abandoned.</param>
    /// <param name="claimedAtUtc">The new LastAttemptAtUtc.</param>
    /// <param name="auditEvent">Written atomically with the claim, so it also counts against the action budget.</param>
    Task<bool> TryReclaimStaleAsync(
        Guid id,
        DateTime staleBeforeUtc,
        DateTime claimedAtUtc,
        AgentConditionEvent auditEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that this row was itself caused by an earlier Klacksy remediation (Etappe 5b cascade
    /// guard). Guarded on CausedByConditionId still being null, so the first attribution wins and a
    /// later tick can never rewrite an existing provenance link. No compare-and-swap on Status: the
    /// marking is orthogonal to the lifecycle and must also stick on a row that moves on afterwards.
    /// </summary>
    Task<bool> TrySetCausedByAsync(Guid id, Guid causedByConditionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The rows of one kind the action dispatcher may still act on - Reported (never claimed) and
    /// Prepared (claimed; possibly an abandoned claim). Ordered the way Etappe 5b prioritises under
    /// scarcity: Severity descending, then DetectedAtUtc ascending, so the oldest of the most severe
    /// findings is served first when the budget cannot cover them all. Capped in the database.
    /// </summary>
    Task<List<AgentCondition>> GetActionableByKindAsync(
        string triggerKind,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// How many action CLAIMS this trigger kind has made since <paramref name="sinceUtc"/>, which is
    /// what the daily action budget and the circuit breaker are measured in. Counted from
    /// agent_condition_events - not from an in-memory counter - so several API instances share one
    /// budget instead of one each. A claim's event is written inside the claim's own transaction, so
    /// the count can never miss a claim that happened, not even one whose compare-and-swap reported a
    /// false negative after committing.
    ///
    /// The join to agent_conditions is unavoidable: the events table has no trigger_kind column of its
    /// own, and its only index is on condition_id. Claims are recognised by the
    /// AgentConditionActionDefaults.ActionClaimDetailPrefix marker on Detail, which is what keeps a
    /// human-driven preparation of the same kind from consuming the automation's budget.
    /// </summary>
    Task<int> CountActionClaimsAsync(
        string triggerKind,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every condition executed since <paramref name="sinceUtc"/>, across all kinds - the input of the
    /// cascade guard, which asks whether a newly detected condition appeared on an entity Klacksy has
    /// just acted on. Read once per tick and matched in memory rather than queried per candidate.
    /// </summary>
    Task<List<AgentCondition>> GetExecutedSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// The Executed rows attached to any of <paramref name="entityIds"/>, scoped to what this caller may
    /// see - the read behind the service grid's "Klacksy handled this one" marker. Ordered stamped rows
    /// before unstamped ones and newest <see cref="AgentCondition.HandledAtUtc"/> first within each, so a
    /// caller that wants one attribution per entity can take the first row it sees per id and is
    /// guaranteed the newest row that actually carries a handling time. That ordering is spelled out with
    /// two keys on purpose - see the implementation for why one DESC key would mean different things on
    /// Postgres and on the EF InMemory provider. Rows carrying no EntityId are excluded, since they can
    /// never be matched to a grid cell. An empty <paramref name="entityIds"/> short-circuits without
    /// touching the database.
    ///
    /// A given entity can legitimately carry SEVERAL Executed rows: the partial unique index on Fingerprint
    /// covers open statuses only, so once a row reaches Executed a re-detection of the same condition opens
    /// a fresh row beside it, and a fingerprint carries the business date, so different days differ anyway.
    /// Collapsing them is the caller's decision, not this method's.
    ///
    /// This is NOT <see cref="GetExecutedSinceAsync"/> with a filter bolted on. That method is unscoped by
    /// design (it feeds the internal cascade guard) and must never be exposed to a user-facing path. It is
    /// also NOT expressible through the planner-relevant scoped reads: their status filter
    /// (AgentConditionPlannerRelevantStatuses.Values) excludes Executed outright, so building on it would
    /// yield an always-empty result. Only the group-scope rule is shared with them, and it is shared by
    /// reusing the same private helper rather than by copying it.
    /// </summary>
    /// <param name="entityIds">The entity ids currently on screen; matched against AgentCondition.EntityId.</param>
    /// <param name="isUnrestricted">True for an admin: every row is returned regardless of GroupId.</param>
    /// <param name="visibleRootIds">Ignored when <paramref name="isUnrestricted"/> is true. Otherwise carries the
    /// same contract as <see cref="GetOpenForScopeAsync"/>: a row is included when its group's Nested Set root is
    /// in this set, or its GroupId is null AND its TriggerKind is not one of AgentTriggerGroupScopedKinds.Values.</param>
    Task<List<AgentCondition>> GetExecutedForEntitiesAsync(
        IReadOnlyCollection<Guid> entityIds,
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves LastSeenAtUtc forward on a still-open row and, when <paramref name="payloadJson"/> is given,
    /// replaces PayloadJson in the same UPDATE. Guarded on the row being open, so an out-of-order write
    /// can never resurrect a terminal row; LastSeenAtUtc itself is written through a GREATEST so the
    /// clock cannot move backwards even when a payload-only refresh gets the row through the filter.
    /// Returns whether a row was updated.
    /// </summary>
    /// <param name="id">The open row to touch.</param>
    /// <param name="seenAtUtc">Detection time of the current tick; only applied when it is newer than the stored one.</param>
    /// <param name="payloadJson">
    /// The detector's current payload, or null to leave the stored one untouched. Non-null makes this a
    /// genuine payload REWRITE - the row's fingerprint, status and every counter stay where they are, so
    /// its memory survives; see IAgentConditionLedgerService.UpsertDetectedAsync for why the ledger stopped
    /// being write-once on this column.
    /// </param>
    Task<bool> TouchLastSeenAsync(
        Guid id,
        DateTime seenAtUtc,
        string? payloadJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a human's single-condition delegation grant (Etappe 4e, "mach du"): DelegatedMaxAction and
    /// DelegatedByUserId, nothing else - this never touches Status, so it carries no compare-and-swap.
    /// Guarded the same way as <see cref="TouchLastSeenAsync"/>, on the row still being
    /// AgentConditionPlannerRelevantStatuses.Values: delegating a row that is Resolved, Rejected or
    /// Executed has nothing left to act on. Returns whether a row was updated.
    /// </summary>
    Task<bool> SetDelegationAsync(
        Guid id,
        ProactiveMaxAction maxAction,
        Guid delegatingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a standalone audit event, for occurrences that are not status transitions (a failed
    /// remediation attempt that leaves Status unchanged, for example). Transitions carry their event
    /// through <see cref="TryTransitionAsync"/> instead, which is atomic with the status change.
    /// </summary>
    Task<AgentConditionEvent> InsertEventAsync(AgentConditionEvent conditionEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns up to <paramref name="take"/> planner-relevant open conditions (Detected, Reported,
    /// Prepared, Escalated - deliberately NOT AgentConditionStateMachine.OpenStatuses, which excludes
    /// Escalated, see that type's remarks) with High or Medium severity, for the per-turn context block
    /// (Etappe 3g). Never loads the full open set: severity, status and scope are filtered and the row
    /// count capped inside the database query itself, since this runs on every chat turn that carries a
    /// user id. <paramref name="isUnrestricted"/> true (Admin) skips the scope filter entirely; otherwise
    /// only rows whose group's Nested Set root is in <paramref name="visibleRootIds"/>, plus rows with no
    /// GroupId whose TriggerKind is not one of AgentTriggerGroupScopedKinds.Values, are eligible - the same
    /// subtree-via-root comparison PlanningAudienceResolver already uses for notification audience
    /// (Etappe 3e), under the same RequiresGroupScope fallback. Ranking: rows whose GroupId equals
    /// <paramref name="preferredGroupId"/> first (ranking only - never widens or narrows visibility), then
    /// Severity descending, then DetectedAtUtc ascending - the same priority order Etappe 5b specifies for
    /// the action dispatcher, reused here for consistency.
    /// </summary>
    Task<IReadOnlyList<AgentCondition>> GetTopForContextAsync(
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        Guid? preferredGroupId,
        int take,
        CancellationToken cancellationToken = default);
}
