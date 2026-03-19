// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller fuer Spam-Regel-Verwaltung (CRUD-Operationen auf SpamRules).
/// </summary>
using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Email;

[ApiController]
[Route("api/backend/ReceivedEmail")]
public class SpamRulesController : BaseController
{
    private readonly IMediator _mediator;

    public SpamRulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("SpamRules")]
    public async Task<ActionResult<List<SpamRuleResource>>> GetSpamRules()
    {
        var result = await _mediator.Send(new GetSpamRulesQuery());
        return Ok(result);
    }

    [HttpPost("SpamRules")]
    public async Task<ActionResult<SpamRuleResource>> CreateSpamRule([FromBody] CreateSpamRuleCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPut("SpamRules/{id:guid}")]
    public async Task<ActionResult<SpamRuleResource>> UpdateSpamRule(Guid id, [FromBody] UpdateSpamRuleCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("SpamRules/{id:guid}")]
    public async Task<ActionResult<bool>> DeleteSpamRule(Guid id)
    {
        var result = await _mediator.Send(new DeleteSpamRuleCommand(id));
        return Ok(result);
    }
}
