// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Result of a pre-commit conflict check: the rule violations that the planned placement(s) would
/// NEWLY introduce (pre-existing violations in the window are excluded via a before/after diff).
/// The caller decides the policy: place_work blocks only on <see cref="HasBlocking"/> (Error, e.g.
/// a collision) and surfaces warnings; find_replacement hard-excludes any candidate with an
/// Error-level conflict (structural or Block-mode escalated) plus its always-excluding pair checks,
/// and attaches Warning-level findings as soft ranking conflicts.
/// <see cref="HasOverridableBlocking"/> / <see cref="HasHardBlocking"/> split the Error set further:
/// an Error produced by Block-mode compliance enforcement (tagged with
/// <see cref="ComplianceRuleNames.EnforcementRuleParamKey"/>) may be overridden by a supervisor. A
/// schedule collision (<see cref="ScheduleValidationKeys.Collision"/>) is Error-level but neither hard
/// nor overridable-blocking under <see cref="HasHardBlocking"/> (owner decision 2026-08-22): the direct
/// write paths (Works Post/Put/BulkAdd/Reassign, WorkChange replacement) no longer refuse the write
/// over it, they persist it and let the async post-commit check
/// (<c>ScheduleTimelineBackgroundService</c>) surface it into the error list like any other finding. A
/// missing mandatory qualification remains hard-blocking under that property - it is the only
/// remaining structural Error that is never overridable and never silently accepted.
/// <see cref="HasNonOverridableBlocking"/> is the pre-2026-08-22 "hard" definition (collision included)
/// and exists solely for <c>CompliancePartitionService</c>'s batch/per-row override gate: that gate
/// mixes rows from MULTIPLE clients in one <see cref="PreCommitCheckResult"/>, so a collision on one
/// client must still stop an authorized override on the whole batch/row from riding a colliding row
/// through - the automated planner/autofill stays conservative even though the direct write paths above
/// no longer are.
/// </summary>
/// <param name="NewConflicts">Violations introduced by the planned rows, not present in the baseline</param>
public sealed record PreCommitCheckResult(IReadOnlyList<ScheduleValidationNotificationDto> NewConflicts)
{
    public bool HasBlocking => NewConflicts.Any(c => c.Type == ScheduleValidationType.Error);

    public bool HasHardBlocking => NewConflicts.Any(IsHardBlocking);

    public bool HasNonOverridableBlocking => NewConflicts.Any(IsNonOverridableBlocking);

    public bool HasOverridableBlocking => NewConflicts.Any(IsOverridableBlocking);

    public bool HasAny => NewConflicts.Count > 0;

    public static PreCommitCheckResult Empty { get; } = new(Array.Empty<ScheduleValidationNotificationDto>());

    private static bool IsHardBlocking(ScheduleValidationNotificationDto entry) =>
        entry.Type == ScheduleValidationType.Error
        && entry.Comment != ScheduleValidationKeys.Collision
        && !IsOverridableBlocking(entry);

    private static bool IsNonOverridableBlocking(ScheduleValidationNotificationDto entry) =>
        entry.Type == ScheduleValidationType.Error && !IsOverridableBlocking(entry);

    private static bool IsOverridableBlocking(ScheduleValidationNotificationDto entry) =>
        entry.Type == ScheduleValidationType.Error
        && entry.CommentParams.ContainsKey(ComplianceRuleNames.EnforcementRuleParamKey);
}
