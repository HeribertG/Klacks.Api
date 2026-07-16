// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules;

public sealed record WarmStartSourceWork(
    Guid AgentId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal WorkTime,
    Guid ShiftId);
