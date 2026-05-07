// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <param name="JobId">The Holistic Harmonizer run whose cached result is materialised.</param>
/// <param name="GroupId">Optional group scope for scenario cloning and name uniqueness.</param>
public sealed record ApplyHolisticHarmonizerAsScenarioRequest(Guid JobId, Guid? GroupId);
