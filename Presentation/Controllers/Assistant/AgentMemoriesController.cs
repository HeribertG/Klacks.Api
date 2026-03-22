// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for agent memory management (CRUD and pin toggle).
/// </summary>
/// <param name="id">The agent ID</param>
/// <param name="memoryId">The memory ID for single operations</param>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/agents")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AgentMemoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentMemoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}/memories")]
    public async Task<IActionResult> GetMemories(
        Guid id, [FromQuery] string? search, [FromQuery] string? category, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAgentMemoriesQuery
        {
            AgentId = id,
            Search = search,
            Category = category
        });
        return Ok(result);
    }

    [HttpPost("{id:guid}/memories")]
    public async Task<IActionResult> CreateMemory(Guid id, [FromBody] CreateMemoryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAgentMemoryCommand
        {
            AgentId = id,
            Key = request.Key,
            Content = request.Content,
            Category = request.Category,
            Importance = request.Importance,
            IsPinned = request.IsPinned,
            ExpiresAt = request.ExpiresAt
        });
        return CreatedAtAction(nameof(GetMemories), new { id }, result);
    }

    [HttpPut("{id:guid}/memories/{memoryId:guid}")]
    public async Task<IActionResult> UpdateMemory(Guid id, Guid memoryId, [FromBody] UpdateMemoryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAgentMemoryCommand
        {
            AgentId = id,
            MemoryId = memoryId,
            Key = request.Key,
            Content = request.Content,
            Category = request.Category,
            Importance = request.Importance,
            IsPinned = request.IsPinned
        });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}/memories/{memoryId:guid}")]
    public async Task<IActionResult> DeleteMemory(Guid id, Guid memoryId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteAgentMemoryCommand { AgentId = id, MemoryId = memoryId });
        return NoContent();
    }

    [HttpPost("{id:guid}/memories/{memoryId:guid}/pin")]
    public async Task<IActionResult> TogglePin(Guid id, Guid memoryId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ToggleMemoryPinCommand { AgentId = id, MemoryId = memoryId });
        if (result == null) return NotFound();
        return Ok(result);
    }
}
