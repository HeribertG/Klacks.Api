// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The visits of one container weekday template as ContainerVisitPlanner places them on an autofill route.
/// </summary>
/// <param name="Items">The template items in route order, each with its visit window and travel times</param>
/// <param name="Stops">The stops in route order as the skill reports them</param>
/// <param name="RouteInfo">The route info the template stores</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record PlannedContainerVisits(
    List<ContainerTemplateItemResource> Items,
    List<ContainerRouteStop> Stops,
    RouteInfo RouteInfo);
