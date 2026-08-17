// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin REST API for a user's absence periods (holidays, sick leave): the escalation roster skips
/// a user while one is active. Not messaging-plugin-gated, not escalation-specific - a plain property
/// of the user.
/// </summary>
/// <param name="mediator">Dispatches the list/create/delete requests.</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.DTOs.Accounts;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Authentification;

[ApiController]
[Route("api/backend/user-absence-periods")]
[Authorize(Roles = Roles.Admin)]
public class UserAbsencePeriodsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserAbsencePeriodsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserAbsencePeriodResource>>> GetByUser([FromQuery] string appUserId)
    {
        var result = await _mediator.Send(new GetUserAbsencePeriodsQuery(appUserId));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserAbsencePeriodResource>> Create([FromBody] CreateUserAbsencePeriodCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<HttpResultResource>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteUserAbsencePeriodCommand(id));
        return Ok(result);
    }
}
