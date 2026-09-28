// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules;

public sealed record WarmStartSourceWork(
    Guid AgentId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal WorkTime,
    Guid ShiftId);
