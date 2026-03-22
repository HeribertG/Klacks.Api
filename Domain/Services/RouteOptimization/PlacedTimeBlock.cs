// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of TimeBlock placement after route optimization.
/// Contains the placed block with calculated start/end time and position in the route.
/// </summary>
/// <param name="Block">The underlying TimeBlock</param>
/// <param name="StartTimeSeconds">Calculated start time in seconds from midnight</param>
/// <param name="EndTimeSeconds">Calculated end time in seconds from midnight</param>
/// <param name="InsertionPosition">Position in the route (index after which shift the block is inserted)</param>

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record PlacedTimeBlock(
    TimeBlock Block,
    double StartTimeSeconds,
    double EndTimeSeconds,
    int InsertionPosition);
