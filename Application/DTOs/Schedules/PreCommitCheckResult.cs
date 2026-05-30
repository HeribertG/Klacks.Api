// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Result of a pre-commit conflict check: the rule violations that the planned placement(s) would
/// NEWLY introduce (pre-existing violations in the window are excluded via a before/after diff).
/// The caller decides the policy: place_work blocks only on <see cref="HasBlocking"/> (Error, e.g.
/// a collision) and surfaces warnings; find_replacement excludes any candidate with <see cref="HasAny"/>.
/// </summary>
/// <param name="NewConflicts">Violations introduced by the planned rows, not present in the baseline</param>
public sealed record PreCommitCheckResult(IReadOnlyList<ScheduleValidationNotificationDto> NewConflicts)
{
    public bool HasBlocking => NewConflicts.Any(c => c.Type == ScheduleValidationType.Error);

    public bool HasAny => NewConflicts.Count > 0;

    public static PreCommitCheckResult Empty { get; } = new(Array.Empty<ScheduleValidationNotificationDto>());
}
