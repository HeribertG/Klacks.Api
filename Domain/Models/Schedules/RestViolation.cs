// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rest period violation between two consecutive work blocks.
/// </summary>
/// <param name="PreviousBlock">The preceding shift</param>
/// <param name="NextBlock">The following shift</param>
/// <param name="ActualRest">Actual rest time between blocks</param>
/// <param name="RequiredRest">Required minimum rest time</param>
namespace Klacks.Api.Domain.Models.Schedules;

public record RestViolation(
    ScheduleBlock PreviousBlock,
    ScheduleBlock NextBlock,
    TimeSpan ActualRest,
    TimeSpan RequiredRest);
