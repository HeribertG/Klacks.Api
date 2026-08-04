// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One candidate period an absence of the requested length could be placed in.
/// </summary>
/// <param name="From">First day of the candidate period</param>
/// <param name="Until">Last day of the candidate period</param>
/// <param name="Fits">True when no evaluated window of this candidate breaks the utilization ceiling</param>
/// <param name="PeakUtilization">Highest utilization across the candidate's windows; null when a window has no capacity left</param>
/// <param name="BlockingWindowCount">How many windows break the ceiling; zero for a fitting candidate</param>

namespace Klacks.Api.Domain.Services.Schedules;

public sealed record CapacityWindowCandidate(
    DateOnly From,
    DateOnly Until,
    bool Fits,
    double? PeakUtilization,
    int BlockingWindowCount);
