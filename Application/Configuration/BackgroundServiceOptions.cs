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
}
