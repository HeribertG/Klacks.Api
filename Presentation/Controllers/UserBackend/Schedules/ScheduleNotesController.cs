// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// Schedule notes. Deliberately on SupervisorDeletableController: the schedule context menu offers the
/// delete without a permission gate of its own, so an Admin-only DELETE would take a supervisor's daily
/// schedule work away.
/// </summary>
public class ScheduleNotesController : SupervisorDeletableController<ScheduleNoteResource>
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
