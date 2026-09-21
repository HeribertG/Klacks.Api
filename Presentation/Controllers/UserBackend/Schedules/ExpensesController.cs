// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// Expenses and reimbursements of a schedule day. Deliberately on SupervisorDeletableController: the
/// schedule context menu offers the delete without a permission gate of its own, so an Admin-only DELETE
/// would take a supervisor's daily schedule work away.
/// </summary>
public class ExpensesController : SupervisorDeletableController<ExpensesResource>
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
