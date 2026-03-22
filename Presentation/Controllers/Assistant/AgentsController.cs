// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for agent management (CRUD, skills, sessions).
/// </summary>
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
public class AgentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllAgentsQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAgentByIdQuery { Id = id });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAgentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAgentCommand
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description
        });
        return CreatedAtAction(nameof(GetById), new { id = ((dynamic)result).Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAgentCommand
        {
            Id = id,
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsActive = request.IsActive
        });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{id:guid}/skills")]
    public async Task<IActionResult> GetSkills(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAgentSkillsQuery { AgentId = id });
        return Ok(result);
    }

    [HttpGet("{id:guid}/sessions")]
    public async Task<IActionResult> GetSessions(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var result = await _mediator.Send(new GetAgentSessionsQuery { AgentId = id, UserId = userId });
        return Ok(result);
    }

    [HttpGet("{id:guid}/sessions/{sessionId:guid}")]
    public async Task<IActionResult> GetSession(Guid id, Guid sessionId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAgentSessionMessagesQuery { AgentId = id, SessionId = sessionId });
        return Ok(result);
    }
}
