// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

/// <summary>
/// A single wizard placement the compliance partition refused to materialise.
/// </summary>
/// <param name="ClientId">Employee the blocked placement would have been assigned to</param>
/// <param name="Date">Workday of the blocked placement</param>
/// <param name="ShiftId">Original (source-world) shift id of the blocked placement, when known</param>
/// <param name="ReasonKey">Translation key of the rule violation that blocked the placement</param>
public sealed record SkippedPlacementDto(
    Guid ClientId,
    DateOnly Date,
    Guid? ShiftId,
    string ReasonKey);
