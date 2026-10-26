// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// An absence entry of the employee that already exists on the calculation date, so the stored hours can be set
/// against the freshly computed ones.
/// </summary>
/// <param name="BreakId">Id of the stored entry</param>
/// <param name="AbsenceId">Absence type of the stored entry</param>
/// <param name="StoredHours">Hours as stored on the entry</param>
/// <param name="HasDirectlyRecordedDuration">True when the entry was recorded as a plain duration, which no macro overwrites</param>
/// <param name="LockLevel">Seal level; a sealed entry is never recalculated in bulk</param>
public sealed record AbsenceHoursExistingEntry(
    Guid BreakId,
    Guid AbsenceId,
    decimal StoredHours,
    bool HasDirectlyRecordedDuration,
    WorkLockLevel LockLevel);
