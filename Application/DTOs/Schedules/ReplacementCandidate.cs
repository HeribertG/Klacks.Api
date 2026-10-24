// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// An employee eligible to cover a slot: no collision/rest violation, not blacklisted, not absent.
/// Aggregate findings (overtime/consecutive/min-rest) are kept as a soft signal — fewer means more
/// rule headroom, so the candidate ranks higher. The target-hours deficit (target minus already
/// assigned hours in the period) is a fairness signal — a larger positive deficit means the employee
/// is further below their target, so they rank higher (Playbook §6.4 step 3b/3c). An employee on call
/// that day ranks ahead of everyone else. A candidate holding a mandatory qualification below a non-binding minimum
/// level stays eligible but ranks after the sufficiently qualified ones (LevelShortfall).
/// </summary>
/// <param name="ClientId">Eligible employee</param>
/// <param name="Name">Display name</param>
/// <param name="IsPreferred">True when the shift is a preferred shift for this employee</param>
/// <param name="SoftConflicts">Non-blocking aggregate findings that lower the ranking</param>
/// <param name="TargetHoursDeficit">Period target hours minus already-assigned hours; positive means below target (ranks higher), zero when no data/target</param>
/// <param name="IsOnCall">True when the employee holds an on-call absence on the date (ranks first)</param>
/// <param name="LevelShortfall">Missing levels over the mandatory qualifications held below a non-binding minimum level; 0 = fully qualified</param>
/// <param name="RhythmDelta">How the cover changes the rhythm of the candidate's own row (the same measure the
/// automatic replacement uses): negative = it closes or extends a block of the same kind, 0 = neutral, positive = a
/// lonely shift, a mixed block or a backward rotation. A tie-break only, right before the target-hours deficit.</param>
/// <param name="Phone">Number to call the candidate on (mobile before fixed line); only filled on the planner endpoint, never in skill output</param>
public sealed record ReplacementCandidate(
    Guid ClientId,
    string Name,
    bool IsPreferred,
    IReadOnlyList<ScheduleValidationNotificationDto> SoftConflicts,
    decimal TargetHoursDeficit,
    bool IsOnCall = false,
    string? Phone = null,
    int LevelShortfall = 0,
    int RhythmDelta = 0);
