// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One atomic repair option. A swap chain gives an agent a shift and takes another away; letting the
/// guardrail judge those halves independently is how a legitimate swap ends up half-applied.
/// </summary>
/// <param name="Rows">Work rows this option would add.</param>
/// <param name="Removals">Intervals this option would vacate.</param>
public sealed record PlannedOption(
    IReadOnlyList<PlannedWorkRow> Rows,
    IReadOnlyList<PlannedRemovalRow> Removals);
