// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/global-rules")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class GlobalRulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public GlobalRulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGlobalRulesQuery());
        return Ok(result);
    }

    [HttpPut("{name}")]
    public async Task<IActionResult> Upsert(string name, [FromBody] UpsertGlobalRuleRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _mediator.Send(new UpsertGlobalRuleCommand
        {
            Name = name,
            Content = request.Content,
            SortOrder = request.SortOrder,
            UserId = userId
        });
        return Ok(result);
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Deactivate(string name, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateGlobalRuleCommand { Name = name });
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetGlobalRulesQuery { IncludeHistory = true, HistoryLimit = limit });
        return Ok(result);
    }
}
