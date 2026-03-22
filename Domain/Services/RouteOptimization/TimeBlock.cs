// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Time block for route optimization (e.g. break).
/// Unmovable blocks have fixed start/end times, movable blocks only have a duration.
/// </summary>
/// <param name="Id">Unique ID of the block</param>
/// <param name="Name">Display name (e.g. "Lunch break")</param>
/// <param name="FixedStartTime">Fixed start time (unmovable only)</param>
/// <param name="FixedEndTime">Fixed end time (unmovable only)</param>
/// <param name="Duration">Duration of the block</param>
/// <param name="IsMovable">True = optimization may move block, False = fixed position</param>

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record TimeBlock(
    Guid Id,
    string Name,
    TimeOnly? FixedStartTime,
    TimeOnly? FixedEndTime,
    TimeSpan Duration,
    bool IsMovable);
