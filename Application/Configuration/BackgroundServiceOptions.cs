// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Configuration for enabling/disabling individual background services.
/// Allows targeted control per API instance during horizontal scaling.
/// </summary>
/// <param name="ScheduleTimeline">Enables the ScheduleTimeline service</param>
/// <param name="PeriodHours">Enables the PeriodHours service</param>
/// <param name="MemoryCleanup">Enables the MemoryCleanup service</param>
/// <param name="Heartbeat">Enables the Heartbeat service</param>
/// <param name="Embedding">Enables the Embedding service</param>
/// <param name="SkillGapSuggestion">Enables the SkillGapSuggestion service</param>
/// <param name="EmailPolling">Enables the EmailPolling service</param>
/// <param name="MessageRetention">Enables the MessageRetention service</param>
/// <param name="LLMModelSync">Enables the LLM model sync service</param>
/// <param name="LLMModelSyncIntervalHours">Interval in hours between sync runs</param>
namespace Klacks.Api.Application.Configuration;

public class BackgroundServiceOptions
{
    public const string SectionName = "BackgroundServices";

    public bool ScheduleTimeline { get; set; } = true;
    public bool PeriodHours { get; set; } = true;
    public bool ThoroughRecalculation { get; set; } = true;
    public bool MemoryCleanup { get; set; } = true;
    public bool Heartbeat { get; set; } = true;
    public bool Embedding { get; set; } = true;
    public bool SkillGapSuggestion { get; set; } = true;
    public bool EmailPolling { get; set; } = true;
    public bool MessageRetention { get; set; } = true;
    public bool DataRetention { get; set; } = true;
    public bool LLMModelSync { get; set; } = true;
    public int LLMModelSyncIntervalHours { get; set; } = 24;
    public bool UpdateDetection { get; set; } = true;

    /// <summary>
    /// Enables the Wizard-4 background anytime-optimizer. Default OFF: it ships dark and is pinned to a
    /// single API instance (the in-process trigger/registry are not cross-instance coordinated yet).
    /// Override per instance via env <c>BackgroundServices__Wizard4=true</c>.
    /// </summary>
    public bool Wizard4 { get; set; } = false;

    /// <summary>
    /// Enables the K15 roster-publication-deadline check. Default OFF: ships dark until reviewed on a
    /// production Work table at scale. Inert per tick (single early-out) on any installation that never
    /// sets COMPLIANCE_ROSTER_PUBLICATION_MIN_LEAD_DAYS &gt; 0. Override via env
    /// <c>BackgroundServices__RosterPublicationCheck=true</c>.
    /// </summary>
    public bool RosterPublicationCheck { get; set; } = false;

    /// <summary>
    /// Enables the WizardRunCapture measurement fallback timer. Default ON: it is inert per tick (a single
    /// query, no writes) whenever no capture is overdue, and only backfills the churn measurement for runs
    /// whose period ended without ever being sealed. The seal event is the primary measurement trigger.
    /// Override via env <c>BackgroundServices__WizardRunCaptureMeasurement=false</c>.
    /// </summary>
    public bool WizardRunCaptureMeasurement { get; set; } = true;

    /// <summary>
    /// Enables the marketplace region-package auto-update check. Default ON: it is inert per cycle
    /// (three settings reads, no HTTP call) on any installation without a recorded
    /// REGION_PACKAGE_COUNTRY/REGION_PACKAGE_VERSION identity. Override via env
    /// <c>BackgroundServices__RegionPackageUpdate=false</c>.
    /// </summary>
    public bool RegionPackageUpdate { get; set; } = true;

    /// <summary>
    /// Enables the goal-reflection background service (Phase 1 shadow mode of the self-directed-goals
    /// feature, see docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md). Default
    /// OFF: Phase 1 only writes reflection results to the log, and shipping it enabled would run an
    /// unmeasured, unattended discovery cycle against production tenants before anyone has opted in.
    /// Override via env <c>BackgroundServices__GoalReflection=true</c>.
    /// </summary>
    public bool GoalReflection { get; set; } = false;

    /// <summary>
    /// Interval in hours between goal-reflection cycles when <see cref="GoalReflection"/> is enabled.
    /// </summary>
    public int GoalReflectionIntervalHours { get; set; } = 24;

    /// <summary>
    /// Switches goal-reflection candidates from Phase 1 shadow mode to Phase 2 delivery (see
    /// docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md). Default OFF: while
    /// off, every candidate is persisted with Status = Shadow exactly as in Phase 1 and nothing is
    /// reachable through the goal-candidates inbox controller. Once on, candidates are persisted with
    /// Status = Proposed and only for users who are planners (Admin/Authorised) — non-planner signals
    /// are skipped rather than surfaced. Override via env
    /// <c>BackgroundServices__GoalReflectionDelivery=true</c>.
    /// </summary>
    public bool GoalReflectionDelivery { get; set; } = false;

    /// <summary>
    /// Triggers the Phase 3 plan draft (see
    /// docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md) as a fire-and-forget
    /// background task right after a human approves a goal candidate. Default OFF: drafting is an LLM
    /// call and, unlike the decision itself, must not run on the request thread; shipping it enabled
    /// before anyone has opted in would draft plans for every approval on every installation. The draft
    /// is display-only — GoalPlanDraftService never executes a step regardless of this flag. Override
    /// via env <c>BackgroundServices__GoalReflectionPlanDrafting=true</c>.
    /// </summary>
    public bool GoalReflectionPlanDrafting { get; set; } = false;

    /// <summary>
    /// Enables Phase 4 unattended execution of a plan drafted from an approved GoalCandidate (see
    /// docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md). Default OFF: even
    /// with a plan already drafted and displayed, GoalPlanExecutionService must never start it running
    /// on an installation that has not explicitly opted into unattended self-reflection execution. When
    /// on, execution is still gated per candidate: exact-High confidence, the minimum autonomy level
    /// across all admin users (no admin, or any admin below Autonomous, blocks execution), and frozen
    /// owner permissions on the candidate — see GoalPlanExecutionService.ExecuteForCandidateAsync.
    /// Override via env <c>BackgroundServices__GoalReflectionExecution=true</c>.
    /// </summary>
    public bool GoalReflectionExecution { get; set; } = false;

    /// <summary>
    /// Enables the plan-approval-timeout background service, which aborts AgentPlan rows that have
    /// been sitting in PausedForApproval longer than <see cref="PlanApprovalTimeoutDays"/>. Default ON:
    /// unlike most background services here, an unattended plan waiting forever for a human approval
    /// that will never come is the worse state to ship dark. The service only ever moves a plan to
    /// Aborted (a terminal, already-supported status) and never touches any other business data, so the
    /// blast radius of running it by default is small. Override via env
    /// <c>BackgroundServices__PlanApprovalTimeout=false</c>.
    /// </summary>
    public bool PlanApprovalTimeout { get; set; } = true;

    /// <summary>
    /// Number of days an AgentPlan may remain in PausedForApproval before
    /// <see cref="PlanApprovalTimeout"/> aborts it.
    /// </summary>
    public int PlanApprovalTimeoutDays { get; set; } = 7;

    /// <summary>
    /// Interval in hours between plan-approval-timeout sweeps when <see cref="PlanApprovalTimeout"/> is
    /// enabled.
    /// </summary>
    public int PlanApprovalTimeoutIntervalHours { get; set; } = 6;

    /// <summary>
    /// Enables the Phase 4 execution-retry sweep (GoalPlanExecutionRetryBackgroundService, see
    /// docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md, section "Kein
    /// Wiederholungsversuch"). Default ON: the sweep starts nothing that
    /// GoalPlanExecutionService.ExecuteForCandidateAsync would not allow on its own - it only
    /// re-attempts candidates whose already-drafted plan never ran because a brake was temporarily
    /// closed at draft time (GoalReflectionExecution was still off, or an admin's autonomy level was
    /// briefly lowered below Autonomous). With GoalReflectionExecution itself off, every re-attempt is
    /// still rejected and logged by that same brake - inert as to effect, not as to log volume. Override
    /// via env <c>BackgroundServices__GoalPlanExecutionRetry=false</c>.
    /// </summary>
    public bool GoalPlanExecutionRetry { get; set; } = true;

    /// <summary>
    /// Interval in hours between goal-plan-execution-retry sweeps when
    /// <see cref="GoalPlanExecutionRetry"/> is enabled.
    /// </summary>
    public int GoalPlanExecutionRetryIntervalHours { get; set; } = 6;

    /// <summary>
    /// Enables the Slack owner bridge: polls inbound Slack messages from the owner's registered
    /// self-alias (APP_OWNER_MESSENGERS) and feeds each one through Klacksy's normal LLM conversation,
    /// sending the reply back to the same Slack DM. Default OFF: this is a brand-new, physically-exposed
    /// entry point into Klacksy (whoever can post to that Slack DM gets Authorised-level access, no
    /// second factor) and must not activate on an installation before the Slack webhook is reachable
    /// and the owner has explicitly configured a Slack self-alias. Inert per tick when no such alias is
    /// configured. Override via env <c>BackgroundServices__SlackOwnerBridge=true</c>.
    /// </summary>
    public bool SlackOwnerBridge { get; set; } = false;

    /// <summary>
    /// Poll interval in seconds between Slack owner bridge cycles when <see cref="SlackOwnerBridge"/>
    /// is enabled.
    /// </summary>
    public int SlackOwnerBridgeIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Enables the group geocoding worker, which sweeps unattempted groups at startup and then drains
    /// the in-process geocoding queue, resolving group addresses to coordinates. Default ON: this is
    /// the behaviour every installation runs today. It is the most expensive service in this list to
    /// run twice: per group it calls Nominatim (a third-party service whose rate limit is shared
    /// across all instances), classifies the place through an LLM provider, and writes the
    /// coordinates. Leave it on for exactly one instance when scaling out.
    /// Caution: <c>IGroupGeocodingQueue</c> stays registered either way, so an instance with the flag
    /// off still accepts enqueue calls from its own request handlers and never drains them - the
    /// channel is bounded and drops the oldest entry, so those groups are silently skipped until the
    /// startup sweep on an enabled instance picks them up.
    /// Override via env <c>BackgroundServices__GroupGeocoding=false</c>.
    /// </summary>
    public bool GroupGeocoding { get; set; } = true;

    /// <summary>
    /// Enables the skill-relation learning worker, which derives and persists relations between
    /// skills. Default ON: it is the behaviour every installation runs today. Pin it to one instance
    /// when scaling out - every instance would derive the same relations from the same shared data
    /// and write them again. Override via env <c>BackgroundServices__SkillRelationLearning=false</c>.
    /// </summary>
    public bool SkillRelationLearning { get; set; } = true;

    /// <summary>
    /// Enables the agent trigger worker, which evaluates the proactive trigger detectors hourly and
    /// dispatches the messages they fire. Default ON: it is the behaviour every installation runs
    /// today. The detectors are database and configuration only, so this costs no LLM tokens, but it
    /// writes a dispatch row per recipient and its dedupe and rate limiting are in-process. Pin it to
    /// one instance when scaling out - otherwise each instance keeps its own rate-limiter state and
    /// users receive the same proactive message once per instance. Override via env
    /// <c>BackgroundServices__AgentTrigger=false</c>.
    /// </summary>
    public bool AgentTrigger { get; set; } = true;

    /// <summary>
    /// Enables the skill-coverage reporter, which compares the documented use cases against the
    /// registered skills once a week. Default ON: it is the behaviour every installation runs today.
    /// It reads a documentation file and writes a single log line - no database writes, no external
    /// call - so running it on every instance costs log volume and nothing else. It is the least
    /// urgent of these flags when scaling out.
    /// Override via env <c>BackgroundServices__SkillCoverage=false</c>.
    /// </summary>
    public bool SkillCoverage { get; set; } = true;

    /// <summary>
    /// Enables the hourly pending-note broadcast cleanup sweep, which expires undelivered broadcast
    /// notes once they are older than a day. The delete is soft, as for every other entity. Default
    /// ON: it is the behaviour every installation runs today. The sweep is idempotent - it selects by
    /// age, so a second instance finds nothing left to expire - but it still writes to shared rows,
    /// so pin it to one instance when scaling out. Override via env
    /// <c>BackgroundServices__PendingNoteBroadcastCleanup=false</c>.
    /// </summary>
    public bool PendingNoteBroadcastCleanup { get; set; } = true;

    /// <summary>
    /// Enables the scheduled-task (cron) worker, which executes the due entries of the ScheduledTasks
    /// table. Default ON: it is the behaviour every installation runs today, and with the flag off on
    /// every instance no cron task runs at all. Unlike the other services here it is already guarded
    /// against duplicate execution: ScheduledTaskRepository.TryClaimAsync advances NextRunUtc with a
    /// single conditional UPDATE, so exactly one instance can claim a due task. The flag therefore
    /// saves polling load rather than preventing double runs. Override via env
    /// <c>BackgroundServices__ScheduledTask=false</c>.
    /// </summary>
    public bool ScheduledTask { get; set; } = true;

    /// <summary>
    /// Enables the ERP order import worker, which picks up dropped ERP order files and imports them.
    /// Default ON: it is the behaviour every installation runs today, and with the flag off on every
    /// instance no ERP order is imported at all. Like the cron worker it is already guarded twice
    /// over: the due-time setting is advanced with a conditional update, and each file is claimed by
    /// an atomic move before it is read. A second instance therefore competes for files rather than
    /// importing them twice, and the flag saves polling load rather than preventing duplicate orders.
    /// Override via env <c>BackgroundServices__ErpOrderImport=false</c>.
    /// </summary>
    public bool ErpOrderImport { get; set; } = true;

    /// <summary>
    /// Enables the knowledge-index startup sync, which brings the shared skill index up to date once
    /// per process start and reports which retrieval stack the process resolved. Default ON: it is the
    /// behaviour every installation runs today, and at least one instance must keep it on or the index
    /// goes stale after a seed change. Pin it to one instance when scaling out - the index lives in the
    /// shared database, and after a deployment that changed the seed every instance would embed the same
    /// content simultaneously, which is paid API traffic whenever the remote embedding provider is
    /// configured. The sync is awaited inside StartAsync, so switching it off also shortens that
    /// instance's startup - at the price of silencing its retrieval-stack diagnostic. Override via env
    /// <c>BackgroundServices__KnowledgeIndexStartup=false</c>.
    /// </summary>
    public bool KnowledgeIndexStartup { get; set; } = true;

    /// <summary>
    /// Enables the daily answer-grounding sentinel probe. Default ON: it is the behaviour every
    /// installation runs today, and it is inert on any installation that leaves
    /// <c>Assistant:AnswerGroundingMode</c> at its Off default - the service returns immediately
    /// without a timer. Where grounding is switched on, the probe writes findings and daily counters
    /// but spends no LLM tokens (the reflection path is skipped for the sentinel agent). Pin it to one
    /// instance when scaling out so the daily counters are not incremented once per instance.
    /// Override via env <c>BackgroundServices__AnswerGroundingSentinel=false</c>.
    /// </summary>
    public bool AnswerGroundingSentinel { get; set; } = true;

    /// <summary>
    /// Enables the escalation-chain sweep (docs/ENTWURF-eskalationskette-2026-08-16.md §5). Default
    /// OFF: it ships dark, same rationale as Wizard4, until the reference-case test and a real
    /// messenger round-trip both hold. UNLIKE most services in this file it is safe to run on EVERY
    /// instance once enabled: it carries no in-process state and every transition is a conditional
    /// ExecuteUpdate, so a second instance racing the same tick loses cleanly instead of double-firing.
    /// Deliberately not pinned, because a single point of failure at 03:00 defeats the chain's purpose.
    /// Override via env <c>BackgroundServices__EscalationChain=true</c>.
    /// </summary>
    public bool EscalationChain { get; set; } = false;

    /// <summary>
    /// Cadence in seconds between escalation-chain sweeps. 30s by default (the Entwurf's documented
    /// compromise: at a 6-minute stage duration the jitter costs up to ~8% of the budget). Unlike
    /// EscalationStage's own min/max/prep-buffer caps this stays here rather than in the runtime
    /// Settings table: changing it needs a restart on every instance anyway, since each instance runs
    /// its own PeriodicTimer.
    /// </summary>
    public int EscalationChainSweepIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Startup delay in SECONDS, not minutes like most services in this file: a restart at 03:05 must
    /// not leave the chain standing while planners wait unnotified.
    /// </summary>
    public int EscalationChainStartupDelaySeconds { get; set; } = 15;
}
