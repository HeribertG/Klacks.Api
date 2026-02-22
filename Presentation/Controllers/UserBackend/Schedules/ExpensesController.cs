// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public class ExpensesController : InputBaseController<ExpensesResource>
{
    public ExpensesController(IMediator mediator, ILogger<ExpensesController> logger)
        : base(mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpensesResource>>> GetList()
    {
        var result = await Mediator.Send(new ListQuery<ExpensesResource>());
        return Ok(result);
    }
}
