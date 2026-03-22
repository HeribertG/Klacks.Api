// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for agent soul management (read, upsert, deactivate sections, history).
/// </summary>
/// <param name="id">The agent ID</param>
/// <param name="sectionType">The soul section type</param>
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
[Route("api/backend/assistant/agents")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AgentSoulController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentSoulController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}/soul")]
    public async Task<IActionResult> GetSoulSections(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSoulSectionsQuery { AgentId = id });
        return Ok(result);
    }

    [HttpPut("{id:guid}/soul/{sectionType}")]
    public async Task<IActionResult> UpsertSoulSection(
        Guid id, string sectionType, [FromBody] UpsertSoulRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _mediator.Send(new UpsertSoulSectionCommand
        {
            AgentId = id,
            SectionType = sectionType,
            Content = request.Content,
            SortOrder = request.SortOrder,
            UserId = userId
        });
        return Ok(result);
    }

    [HttpDelete("{id:guid}/soul/{sectionType}")]
    public async Task<IActionResult> DeactivateSoulSection(Guid id, string sectionType, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateSoulSectionCommand { AgentId = id, SectionType = sectionType });
        return NoContent();
    }

    [HttpGet("{id:guid}/soul/history")]
    public async Task<IActionResult> GetSoulHistory(Guid id, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSoulHistoryQuery { AgentId = id, Limit = limit });
        return Ok(result);
    }
}
