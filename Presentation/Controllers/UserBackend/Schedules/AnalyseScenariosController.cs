// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for CRUD operations and status changes of AnalyseScenarios.
/// </summary>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries.AnalyseScenarios;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

[ApiController]
public class AnalyseScenariosController : BaseController
{
    private readonly IMediator _mediator;

    public AnalyseScenariosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<AnalyseScenarioResource>>> GetList([FromQuery] Guid? groupId)
    {
        var result = await _mediator.Send(new ListAnalyseScenariosQuery(groupId));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AnalyseScenarioResource>> Get(Guid id)
    {
        var result = await _mediator.Send(new GetAnalyseScenarioQuery(id));

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AnalyseScenarioResource>> Create([FromBody] CreateAnalyseScenarioRequest request)
    {
        var result = await _mediator.Send(new CreateAnalyseScenarioCommand(request));
        return Ok(result);
    }

    [HttpPost("{id}/Accept")]
    public async Task<ActionResult<bool>> Accept(Guid id)
    {
        var result = await _mediator.Send(new AcceptAnalyseScenarioCommand(id));
        return Ok(result);
    }

    [HttpPost("{id}/Reject")]
    public async Task<ActionResult<bool>> Reject(Guid id)
    {
        var result = await _mediator.Send(new RejectAnalyseScenarioCommand(id));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteAnalyseScenarioCommand(id));
        return Ok(result);
    }

    [HttpPatch("{id}/Rename")]
    public async Task<ActionResult<AnalyseScenarioResource>> Rename(Guid id, [FromBody] RenameAnalyseScenarioRequest request)
    {
        var result = await _mediator.Send(new RenameAnalyseScenarioCommand(id, request.Name));
        return Ok(result);
    }
}
