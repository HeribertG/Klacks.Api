// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record PlannedWorkDeletion(
    Guid WorkId,
    DateOnly Date,
    Guid ShiftId,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime);
