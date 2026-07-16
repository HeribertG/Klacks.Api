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
}
