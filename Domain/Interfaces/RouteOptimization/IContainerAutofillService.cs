// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service for automatic filling of container templates with matching shifts.
/// Solves an Orienteering Problem: select a subset of shifts within the time budget and optimize the route.
/// </summary>
/// <param name="request">Contains container ID, time window, base addresses and transport mode</param>

using Klacks.Api.Domain.Services.RouteOptimization;

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public interface IContainerAutofillService
{
    Task<ContainerAutofillResult> AutofillAsync(ContainerAutofillRequest request);
}
