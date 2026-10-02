// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <param name="JobId">The Holistic Harmonizer run whose cached result is materialised.</param>
/// <param name="GroupId">Optional group scope for scenario cloning and name uniqueness.</param>
/// <param name="Language">The planner's language for the scenario name prefix; null falls back to the installation language.</param>
public sealed record ApplyHolisticHarmonizerAsScenarioRequest(Guid JobId, Guid? GroupId, string? Language = null);
