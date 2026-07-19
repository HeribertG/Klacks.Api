// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

/// <summary>
/// Request to apply a cached wizard result as a new scenario.
/// </summary>
/// <param name="JobId">The job whose cached result is materialised.</param>
/// <param name="GroupId">Optional group scope for scenario cloning and name uniqueness.</param>
/// <param name="OverrideBlock">Requests the K1 supervisor override for Block-mode compliance violations.</param>
public sealed record ApplyAsScenarioRequest(Guid JobId, Guid? GroupId, bool OverrideBlock = false);
