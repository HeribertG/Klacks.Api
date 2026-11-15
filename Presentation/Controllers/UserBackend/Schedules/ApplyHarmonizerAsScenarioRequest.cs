// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// Request body to materialise the cached result of a harmonizer job as a new scenario.
/// </summary>
/// <param name="JobId">The harmonizer job whose cached result is materialised</param>
/// <param name="GroupId">Optional group scope for scenario cloning and name uniqueness</param>
/// <param name="Language">The planner's language for the scenario name prefix; null falls back to the installation language</param>
public sealed record ApplyHarmonizerAsScenarioRequest(Guid JobId, Guid? GroupId, string? Language = null);
