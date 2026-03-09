// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public class ScheduleNotesController : InputBaseController<ScheduleNoteResource>
{
    public ScheduleNotesController(IMediator mediator, ILogger<ScheduleNotesController> logger)
        : base(mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduleNoteResource>>> GetList()
    {
        var result = await Mediator.Send(new ListQuery<ScheduleNoteResource>());
        return Ok(result);
    }
}
