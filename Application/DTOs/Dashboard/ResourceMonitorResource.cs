// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Full-year resource monitor payload containing one entry per calendar day.
/// </summary>
namespace Klacks.Api.Application.DTOs.Dashboard;

public class ResourceMonitorResource
{
    public IEnumerable<ResourceMonitorDayResource> DailyData { get; set; } = [];
}
