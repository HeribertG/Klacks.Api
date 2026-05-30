// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A single proposed assignment to be materialized into a scenario by propose_plan.
/// </summary>
/// <param name="ClientId">Employee to assign</param>
/// <param name="ShiftId">Shift to assign them to</param>
/// <param name="Date">Workday</param>
public sealed record PlacementInput(Guid ClientId, Guid ShiftId, DateOnly Date);
