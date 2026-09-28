// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules;

/// <param name="ShiftId">The shift definition reference</param>
/// <param name="Date">The calendar date the shift occupies</param>
/// <param name="Start">Shift start time of day</param>
/// <param name="End">Shift end time of day</param>
public readonly record struct AvailabilityShiftSlot(Guid ShiftId, DateOnly Date, TimeOnly Start, TimeOnly End);
