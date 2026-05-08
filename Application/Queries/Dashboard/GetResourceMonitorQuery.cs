// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query to retrieve per-day target and actual hours for the resource monitor dashboard.
/// </summary>
/// <param name="Year">Calendar year to compute (e.g. 2026)</param>
/// <param name="GroupId">Optional group filter — null means all employees</param>
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Dashboard;

public record GetResourceMonitorQuery(int Year, Guid? GroupId) : IRequest<ResourceMonitorResource>;
