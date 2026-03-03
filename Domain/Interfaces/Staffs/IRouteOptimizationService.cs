// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.RouteOptimization;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IRouteOptimizationService
{
    Task<DistanceMatrix> CalculateDistanceMatrixAsync(Guid containerId, int weekday, bool isHoliday, ContainerTransportMode transportMode = ContainerTransportMode.ByCar);
    Task<RouteOptimizationResult> OptimizeRouteAsync(Guid containerId, int weekday, bool isHoliday, string? startBase = null, string? endBase = null, ContainerTransportMode transportMode = ContainerTransportMode.ByCar);
}
