// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A stop on an optimized route: address, coordinates, the shift it serves and the time spent on site.
/// </summary>
/// <param name="TotalOnSiteTime">Briefing, work and debriefing time added up</param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record Location
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public Guid ShiftId { get; init; }
    public TransportMode TransportMode { get; init; } = TransportMode.ByCar;
    public TimeSpan BriefingTime { get; init; } = TimeSpan.Zero;
    public TimeSpan DebriefingTime { get; init; } = TimeSpan.Zero;
    public TimeSpan WorkTime { get; init; } = TimeSpan.Zero;
    public TimeSpan TotalOnSiteTime => BriefingTime + WorkTime + DebriefingTime;
    public TimeOnly? TimeRangeStart { get; init; }
    public TimeOnly? TimeRangeEnd { get; init; }
}
