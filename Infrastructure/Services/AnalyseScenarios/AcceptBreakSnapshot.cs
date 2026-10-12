// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read-only projection of a break used by the accept day-lock plan and the skip count.
/// </summary>
/// <param name="Id">Break id</param>
/// <param name="ClientId">Employee of the break</param>
/// <param name="CurrentDate">Day of the break</param>
/// <param name="AbsenceId">Absence type</param>
/// <param name="StartTime">Start time</param>
/// <param name="EndTime">End time</param>
/// <param name="LockLevel">Lock level of the row</param>
/// <param name="ParentWorkId">Work the break pauses, null for a standalone absence</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal sealed record AcceptBreakSnapshot(
    Guid Id,
    Guid ClientId,
    DateOnly CurrentDate,
    Guid AbsenceId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    WorkLockLevel LockLevel,
    Guid? ParentWorkId);
