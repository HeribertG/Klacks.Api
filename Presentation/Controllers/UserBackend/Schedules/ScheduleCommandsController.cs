// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for CRUD operations on schedule commands (FREE, EARLY, LATE, NIGHT keywords).
/// </summary>
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public class ScheduleCommandsController : InputBaseController<ScheduleCommandResource>
{
    public ScheduleCommandsController(IMediator mediator, ILogger<ScheduleCommandsController> logger)
        : base(mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduleCommandResource>>> GetList()
    {
        var result = await Mediator.Send(new ListQuery<ScheduleCommandResource>());
        return Ok(result);
    }
}
