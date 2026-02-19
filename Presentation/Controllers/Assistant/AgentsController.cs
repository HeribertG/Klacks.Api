using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Constants;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/agents")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AgentsController : ControllerBase
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSoulRepository _soulRepository;
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IAgentSkillRepository _skillRepository;
    private readonly IAgentSessionRepository _sessionRepository;
    private readonly IEmbeddingService _embeddingService;

    public AgentsController(
        IAgentRepository agentRepository,
        IAgentSoulRepository soulRepository,
        IAgentMemoryRepository memoryRepository,
        IAgentSkillRepository skillRepository,
        IAgentSessionRepository sessionRepository,
        IEmbeddingService embeddingService)
    {
        _agentRepository = agentRepository;
        _soulRepository = soulRepository;
        _memoryRepository = memoryRepository;
        _skillRepository = skillRepository;
        _sessionRepository = sessionRepository;
        _embeddingService = embeddingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var agents = await _agentRepository.GetAllAsync(ct);
        return Ok(agents.Select(a => new
        {
            a.Id, a.Name, a.DisplayName, a.Description,
            a.IsActive, a.IsDefault, a.CreateTime
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByIdAsync(id, ct);
        if (agent == null) return NotFound();

        var sections = await _soulRepository.GetActiveSectionsAsync(id, ct);
        var skills = await _skillRepository.GetEnabledAsync(id, ct);

        return Ok(new
        {
            agent.Id, agent.Name, agent.DisplayName, agent.Description,
            agent.IsActive, agent.IsDefault, agent.CreateTime,
            SoulSections = sections.Select(s => new { s.Id, s.SectionType, s.SortOrder, s.Version }),
            SkillCount = skills.Count
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAgentRequest request, CancellationToken ct)
    {
        var agent = new Agent
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsActive = true,
            IsDefault = false
        };

        await _agentRepository.AddAsync(agent, ct);
        return CreatedAtAction(nameof(GetById), new { id = agent.Id }, new { agent.Id, agent.Name });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentRequest request, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByIdAsync(id, ct);
        if (agent == null) return NotFound();

        if (request.Name != null) agent.Name = request.Name;
        if (request.DisplayName != null) agent.DisplayName = request.DisplayName;
        if (request.Description != null) agent.Description = request.Description;
        if (request.IsActive.HasValue) agent.IsActive = request.IsActive.Value;

        await _agentRepository.UpdateAsync(agent, ct);
        return Ok(new { agent.Id, agent.Name, agent.DisplayName, agent.IsActive });
    }

    // ── Soul Sections ─────────────────────────────────────────

    [HttpGet("{id:guid}/soul")]
    public async Task<IActionResult> GetSoulSections(Guid id, CancellationToken ct)
    {
        var sections = await _soulRepository.GetActiveSectionsAsync(id, ct);
        return Ok(sections.Select(s => new
        {
            s.Id, s.SectionType, s.Content, s.SortOrder,
            s.IsActive, s.Version, s.Source, s.CreateTime
        }));
    }

    [HttpPut("{id:guid}/soul/{sectionType}")]
    public async Task<IActionResult> UpsertSoulSection(
        Guid id, string sectionType, [FromBody] UpsertSoulRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _soulRepository.UpsertSectionAsync(
            id, sectionType, request.Content,
            request.SortOrder ?? GetDefaultSortOrder(sectionType),
            source: null, changedBy: userId, cancellationToken: ct);

        return Ok(new { AgentId = id, SectionType = sectionType });
    }

    [HttpDelete("{id:guid}/soul/{sectionType}")]
    public async Task<IActionResult> DeactivateSoulSection(Guid id, string sectionType, CancellationToken ct)
    {
        await _soulRepository.DeactivateSectionAsync(id, sectionType, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/soul/history")]
    public async Task<IActionResult> GetSoulHistory(Guid id, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var history = await _soulRepository.GetHistoryAsync(id, limit, ct);
        return Ok(history.Select(h => new
        {
            h.Id, h.SectionType, h.ContentBefore, h.ContentAfter,
            h.Version, h.ChangeType, h.ChangedBy, h.CreateTime
        }));
    }

    // ── Memories ──────────────────────────────────────────────

    [HttpGet("{id:guid}/memories")]
    public async Task<IActionResult> GetMemories(
        Guid id, [FromQuery] string? search, [FromQuery] string? category, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var queryEmbedding = _embeddingService.IsAvailable
                ? await _embeddingService.GenerateEmbeddingAsync(search, ct)
                : null;

            var results = await _memoryRepository.HybridSearchAsync(id, search, queryEmbedding, 20, ct);
            return Ok(results);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var memories = await _memoryRepository.GetByCategoryAsync(id, category, ct);
            return Ok(memories.Select(MapMemory));
        }

        var all = await _memoryRepository.GetAllAsync(id, ct);
        return Ok(all.Select(MapMemory));
    }

    [HttpPost("{id:guid}/memories")]
    public async Task<IActionResult> CreateMemory(Guid id, [FromBody] CreateMemoryRequest request, CancellationToken ct)
    {
        var memory = new AgentMemory
        {
            AgentId = id,
            Category = request.Category ?? MemoryCategories.Fact,
            Key = request.Key,
            Content = request.Content,
            Importance = Math.Clamp(request.Importance ?? 5, 1, 10),
            IsPinned = request.IsPinned ?? false,
            Source = MemorySources.UserExplicit,
            ExpiresAt = request.ExpiresAt
        };

        if (_embeddingService.IsAvailable)
        {
            memory.Embedding = await _embeddingService.GenerateEmbeddingAsync(
                $"{memory.Key}: {memory.Content}", ct);
        }

        await _memoryRepository.AddAsync(memory, ct);
        return CreatedAtAction(nameof(GetMemories), new { id }, MapMemory(memory));
    }

    [HttpPut("{id:guid}/memories/{memoryId:guid}")]
    public async Task<IActionResult> UpdateMemory(Guid id, Guid memoryId, [FromBody] UpdateMemoryRequest request, CancellationToken ct)
    {
        var memory = await _memoryRepository.GetByIdAsync(memoryId, ct);
        if (memory == null || memory.AgentId != id) return NotFound();

        var contentChanged = false;
        if (request.Key != null) { memory.Key = request.Key; contentChanged = true; }
        if (request.Content != null) { memory.Content = request.Content; contentChanged = true; }
        if (request.Category != null) memory.Category = request.Category;
        if (request.Importance.HasValue) memory.Importance = Math.Clamp(request.Importance.Value, 1, 10);
        if (request.IsPinned.HasValue) memory.IsPinned = request.IsPinned.Value;

        if (contentChanged && _embeddingService.IsAvailable)
        {
            memory.Embedding = await _embeddingService.GenerateEmbeddingAsync(
                $"{memory.Key}: {memory.Content}", ct);
        }

        await _memoryRepository.UpdateAsync(memory, ct);
        return Ok(MapMemory(memory));
    }

    [HttpDelete("{id:guid}/memories/{memoryId:guid}")]
    public async Task<IActionResult> DeleteMemory(Guid id, Guid memoryId, CancellationToken ct)
    {
        var memory = await _memoryRepository.GetByIdAsync(memoryId, ct);
        if (memory == null || memory.AgentId != id) return NotFound();

        await _memoryRepository.DeleteAsync(memoryId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/memories/{memoryId:guid}/pin")]
    public async Task<IActionResult> TogglePin(Guid id, Guid memoryId, CancellationToken ct)
    {
        var memory = await _memoryRepository.GetByIdAsync(memoryId, ct);
        if (memory == null || memory.AgentId != id) return NotFound();

        memory.IsPinned = !memory.IsPinned;
        await _memoryRepository.UpdateAsync(memory, ct);
        return Ok(new { memory.Id, memory.IsPinned });
    }

    // ── Skills ────────────────────────────────────────────────

    [HttpGet("{id:guid}/skills")]
    public async Task<IActionResult> GetSkills(Guid id, CancellationToken ct)
    {
        var skills = await _skillRepository.GetEnabledAsync(id, ct);
        return Ok(skills.Select(s => new
        {
            s.Id, s.Name, s.Description, s.Category,
            s.IsEnabled, s.SortOrder, s.ExecutionType, s.Version
        }));
    }

    // ── Sessions ──────────────────────────────────────────────

    [HttpGet("{id:guid}/sessions")]
    public async Task<IActionResult> GetSessions(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var sessions = await _sessionRepository.GetUserSessionsAsync(userId, 50, ct);
        return Ok(sessions.Select(s => new
        {
            s.Id, s.SessionId, s.Title, s.Status,
            s.MessageCount, s.TokenCountEst, s.LastMessageAt, s.IsArchived
        }));
    }

    [HttpGet("{id:guid}/sessions/{sessionId:guid}")]
    public async Task<IActionResult> GetSession(Guid id, Guid sessionId, CancellationToken ct)
    {
        var messages = await _sessionRepository.GetActiveMessagesAsync(sessionId, ct);
        return Ok(messages.Select(m => new
        {
            m.Id, m.Role, m.Content, m.TokenCount,
            m.ModelId, m.FunctionCalls, m.CreateTime
        }));
    }

    private static object MapMemory(AgentMemory m) => new
    {
        m.Id, m.Key, m.Content, m.Category, m.Importance,
        m.IsPinned, m.Source, m.ExpiresAt, m.AccessCount, m.CreateTime
    };

    private static int GetDefaultSortOrder(string sectionType) => sectionType switch
    {
        SoulSectionTypes.Identity => 0,
        SoulSectionTypes.Personality => 1,
        SoulSectionTypes.Tone => 2,
        SoulSectionTypes.Boundaries => 3,
        SoulSectionTypes.CommunicationStyle => 4,
        SoulSectionTypes.Values => 5,
        SoulSectionTypes.DomainExpertise => 6,
        SoulSectionTypes.ErrorHandling => 7,
        SoulSectionTypes.UserContext => 8,
        SoulSectionTypes.GroupBehavior => 9,
        _ => 99
    };
}

public record CreateAgentRequest(string Name, string? DisplayName, string? Description);
public record UpdateAgentRequest(string? Name, string? DisplayName, string? Description, bool? IsActive);
public record UpsertSoulRequest(string Content, int? SortOrder);
public record CreateMemoryRequest(string Key, string Content, string? Category, int? Importance, bool? IsPinned, DateTime? ExpiresAt);
public record UpdateMemoryRequest(string? Key, string? Content, string? Category, int? Importance, bool? IsPinned);
