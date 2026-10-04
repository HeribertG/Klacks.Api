// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Daily rest violation between two consecutive work days (see ClientTimeline.GetRestGaps).
/// </summary>
/// <param name="PreviousBlock">The work block with the latest end of the earlier work day</param>
/// <param name="NextBlock">The first work block of the following work day</param>
/// <param name="ActualRest">Actual rest time between blocks</param>
/// <param name="RequiredRest">Required minimum rest time</param>
namespace Klacks.Api.Domain.Models.Schedules;

public record RestViolation(
    ScheduleBlock PreviousBlock,
    ScheduleBlock NextBlock,
    TimeSpan ActualRest,
    TimeSpan RequiredRest);
