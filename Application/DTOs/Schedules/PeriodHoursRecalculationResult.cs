// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of a manual period-hours recalculation (normal or thorough).
/// </summary>
/// <param name="Accepted">True when the recalculation ran (normal) or was queued (thorough)</param>
/// <param name="FrozenSkipped">Number of frozen client pay periods (sealed and exported, behind the recalculation
/// window) that were left unchanged; 0 when the admin override included them</param>
/// <param name="NothingToRecalculate">True when the caller may see nobody in the window, so nothing ran or was queued
/// and no completion follows</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public record PeriodHoursRecalculationResult(bool Accepted, int FrozenSkipped, bool NothingToRecalculate = false);
