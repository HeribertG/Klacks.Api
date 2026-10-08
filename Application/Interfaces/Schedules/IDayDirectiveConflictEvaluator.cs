// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Reports planned rows that contradict the day directives (schedule commands FREE / EARLY / -EARLY / ...) of their
/// employee: an ABSOLUTE per-(client, date) check, several commands of one day combined cumulatively.
/// </summary>
public interface IDayDirectiveConflictEvaluator
{
    Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedAsync(
        IReadOnlyList<PlannedWorkRow> plannedRows,
        Guid? analyseToken,
        CancellationToken cancellationToken = default);
}
