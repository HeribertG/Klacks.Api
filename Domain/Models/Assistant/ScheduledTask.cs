// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// A recurring (cron) task Klacksy executes on a schedule on behalf of its owner. The action is
/// resolved to a concrete artifact at authoring time (a reminder text or a deterministic skill
/// invocation) so the fire-time run needs no LLM and no further user input. The owner's identity and
/// permission snapshot are captured so the scheduled run executes under the owner with consent given
/// when the schedule was created.
/// </summary>
public class ScheduledTask : BaseEntity
{
    /// <summary>Human-readable label, unique per owner among non-deleted tasks.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Standard 5-field cron expression (minute hour day-of-month month day-of-week).</summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>IANA time zone the cron expression is interpreted in (e.g. "Europe/Zurich").</summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>What the task does when it fires; see ScheduledTaskActionTypes.</summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>The message delivered to the owner when ActionType is "reminder".</summary>
    public string? MessageText { get; set; }

    /// <summary>The skill executed when ActionType is "skill".</summary>
    public string? SkillName { get; set; }

    /// <summary>JSON object of parameters passed to the skill when ActionType is "skill".</summary>
    public string ParametersJson { get; set; } = "{}";

    /// <summary>The user that owns the task and receives its results.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Display name of the owner, captured at authoring time.</summary>
    public string OwnerUserName { get; set; } = string.Empty;

    /// <summary>Comma-separated snapshot of the owner's permissions, enforced when the skill runs.</summary>
    public string OwnerPermissionsCsv { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    /// <summary>Next planned run in UTC; null when the schedule has no further occurrence.</summary>
    public DateTime? NextRunUtc { get; set; }

    public DateTime? LastRunUtc { get; set; }

    /// <summary>Outcome of the last run; see ScheduledTaskRunStatus.</summary>
    public string? LastStatus { get; set; }

    /// <summary>Truncated message of the last run for diagnostics.</summary>
    public string? LastResult { get; set; }

    public int RunCount { get; set; }

    /// <summary>Optional cap on total runs; null means unlimited. The task disables itself when reached.</summary>
    public int? MaxRuns { get; set; }

    /// <summary>
    /// Explicit per-task opt-in that lets an irreversible skill run unattended. Off by default: a
    /// background run has nobody to confirm anything, so the destructive classes stay refused unless the
    /// owner deliberately accepted that for this one task.
    /// </summary>
    public bool AllowIrreversibleUnattended { get; set; }

    /// <summary>
    /// Set when a run was refused for a cause the owner can still fix. Deliberately separate from
    /// IsEnabled, which carries the owner's own on/off intent: lifting a pause must not have to guess
    /// whether the task was switched off by its owner or by the policy.
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>Why the task was paused; null while it is not paused. Never set directly - the two
    /// pause fields are only ever moved together through Pause and ClearPause, so a stale reason can
    /// not survive next to IsPaused=false.</summary>
    public string? PausedReason { get; private set; }

    /// <summary>
    /// Pauses the task for a cause its owner can still fix, keeping IsEnabled and the schedule.
    /// </summary>
    /// <param name="reason">Human-readable cause shown to the owner when the pause is explained.</param>
    public void Pause(string reason)
    {
        IsPaused = true;
        PausedReason = reason;
    }

    /// <summary>
    /// Lifts the pause and drops its reason in one step, so the reason can never outlive the pause.
    /// </summary>
    public void ClearPause()
    {
        IsPaused = false;
        PausedReason = null;
    }
}
