// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Result of a pre-commit conflict check: the rule violations that the planned placement(s) would
/// NEWLY introduce (pre-existing violations in the window are excluded via a before/after diff).
/// The caller decides the policy: place_work blocks only on <see cref="HasBlocking"/> (Error, e.g.
/// a collision) and surfaces warnings; find_replacement excludes any candidate with <see cref="HasAny"/>.
/// <see cref="HasOverridableBlocking"/> / <see cref="HasHardBlocking"/> split the Error set further:
/// an Error produced by Block-mode compliance enforcement (tagged with
/// <see cref="ComplianceRuleNames.EnforcementRuleParamKey"/>) may be overridden by a supervisor;
/// a structural Error (collision, missing mandatory qualification) never can be.
/// </summary>
/// <param name="NewConflicts">Violations introduced by the planned rows, not present in the baseline</param>
public sealed record PreCommitCheckResult(IReadOnlyList<ScheduleValidationNotificationDto> NewConflicts)
{
    public bool HasBlocking => NewConflicts.Any(c => c.Type == ScheduleValidationType.Error);

    public bool HasHardBlocking => NewConflicts.Any(IsHardBlocking);

    public bool HasOverridableBlocking => NewConflicts.Any(IsOverridableBlocking);

    public bool HasAny => NewConflicts.Count > 0;

    public static PreCommitCheckResult Empty { get; } = new(Array.Empty<ScheduleValidationNotificationDto>());

    private static bool IsHardBlocking(ScheduleValidationNotificationDto entry) =>
        entry.Type == ScheduleValidationType.Error && !IsOverridableBlocking(entry);

    private static bool IsOverridableBlocking(ScheduleValidationNotificationDto entry) =>
        entry.Type == ScheduleValidationType.Error
        && entry.CommentParams.ContainsKey(ComplianceRuleNames.EnforcementRuleParamKey);
}
