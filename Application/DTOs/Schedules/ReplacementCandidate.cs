// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// An employee eligible to cover a slot: no collision/rest violation, not blacklisted, not absent.
/// Aggregate findings (overtime/consecutive/min-rest) are kept as a soft signal — fewer means more
/// rule headroom, so the candidate ranks higher.
/// </summary>
/// <param name="ClientId">Eligible employee</param>
/// <param name="Name">Display name</param>
/// <param name="IsPreferred">True when the shift is a preferred shift for this employee</param>
/// <param name="SoftConflicts">Non-blocking aggregate findings that lower the ranking</param>
public sealed record ReplacementCandidate(
    Guid ClientId,
    string Name,
    bool IsPreferred,
    IReadOnlyList<ScheduleValidationNotificationDto> SoftConflicts);
