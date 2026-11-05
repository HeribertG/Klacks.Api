// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The display switches open_schedule turns on in the schedule page.
/// </summary>
/// <param name="Availability">Show the availability overlay of the employees</param>
/// <param name="Commands">Show the planning commands (keywords) in the cells</param>

namespace Klacks.Api.Application.Skills;

internal sealed record ScheduleDisplays(bool Availability, bool Commands);
