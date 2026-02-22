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
