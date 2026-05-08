// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for dashboard data: employee locations, shift coverage statistics, and resource monitor.
/// </summary>

using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

[ApiController]
public class DashboardController : BaseController
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("ClientLocations")]
    public async Task<ActionResult<IEnumerable<ClientLocationResource>>> GetClientLocations()
    {
        var locations = await _mediator.Send(new GetClientLocationsQuery());
        return Ok(locations);
    }

    [HttpGet("ShiftCoverageStatistics")]
    public async Task<ActionResult<IEnumerable<ShiftCoverageStatisticsResource>>> GetShiftCoverageStatistics()
    {
        var statistics = await _mediator.Send(new GetShiftCoverageStatisticsQuery());
        return Ok(statistics);
    }

    [HttpGet("ResourceMonitor")]
    public async Task<ActionResult<ResourceMonitorResource>> GetResourceMonitor(
        [FromQuery] int year,
        [FromQuery] Guid? groupId = null)
    {
        var resource = await _mediator.Send(new GetResourceMonitorQuery(year, groupId));
        return Ok(resource);
    }
}
