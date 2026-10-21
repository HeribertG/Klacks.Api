// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <param name="JobId">The harmonizer job whose cached result is materialised</param>
/// <param name="GroupId">Optional group scope for scenario cloning and name uniqueness</param>
/// <param name="Language">The planner's language for the scenario name prefix; null falls back to the installation language</param>

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public sealed record ApplyHarmonizerAsScenarioRequest(Guid JobId, Guid? GroupId, string? Language = null);
