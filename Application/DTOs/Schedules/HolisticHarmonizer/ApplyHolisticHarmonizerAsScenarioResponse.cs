// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

public sealed record ApplyHolisticHarmonizerAsScenarioResponse(
    Guid ScenarioId,
    Guid ScenarioToken,
    string ScenarioName,
    Guid? RunGroupId,
    IReadOnlyList<Guid> CreatedWorkIds);
