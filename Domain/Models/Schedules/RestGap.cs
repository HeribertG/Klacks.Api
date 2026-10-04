// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rest period between two consecutive work days of one client (the pauses inside a work day are not rest gaps).
/// </summary>
/// <param name="PreviousBlock">The work block with the latest end of the earlier work day; its End is where the rest starts</param>
/// <param name="NextBlock">The first work block of the following work day; its Start is where the rest ends</param>
/// <param name="Duration">Length of the rest between the two work days</param>
namespace Klacks.Api.Domain.Models.Schedules;

public record RestGap(
    ScheduleBlock PreviousBlock,
    ScheduleBlock NextBlock,
    TimeSpan Duration);
