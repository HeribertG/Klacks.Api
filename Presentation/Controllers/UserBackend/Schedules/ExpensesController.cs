// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Expenses and reimbursements of a schedule day. Recording, correcting and deleting them is open to every
/// authenticated user (the planner floor), like the Work they belong to, so the payroll export stays complete
/// when a planner without a role plans the day; group visibility, day locks and the expense seal are enforced by
/// the handlers. The controller is on BaseController rather than a generic CRUD base because an [Authorize] on an
/// override can only narrow the base gate, never widen it.
/// </summary>
/// <param name="mediator">Dispatches the generic CRUD commands and queries for ExpensesResource</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

[ApiController]
public class ExpensesController : BaseController, ICrudResourceController<ExpensesResource>
{
    private readonly IMediator _mediator;

    public ExpensesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpensesResource>>> GetList()
    {
        var result = await _mediator.Send(new ListQuery<ExpensesResource>());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExpensesResource>> Get([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new GetQuery<ExpensesResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpPost]
    public async Task<ActionResult<ExpensesResource>> Post([FromBody] ExpensesResource resource)
    {
        var model = await _mediator.Send(new PostCommand<ExpensesResource>(resource));
        return Ok(model);
    }

    [HttpPut]
    public async Task<ActionResult<ExpensesResource>> Put([FromBody] ExpensesResource resource)
    {
        var model = await _mediator.Send(new PutCommand<ExpensesResource>(resource));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ExpensesResource>> Delete(Guid id)
    {
        var model = await _mediator.Send(new DeleteCommand<ExpensesResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }
}
