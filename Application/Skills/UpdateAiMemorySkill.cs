// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that changes one persistent memory entry. Who may change it is decided by
/// AgentMemoryAccessPolicy, the same rule the REST path applies: a shared company memory only by an
/// administrator, a personal memory only by the user it belongs to — an administrator included. A
/// denied update answers like a missing memory so the skill never confirms that a foreign memory exists.
/// </summary>
/// <param name="agentMemoryRepository">Loads and persists the memory</param>
/// <param name="embeddingService">Regenerates the embedding when key or content changed</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_ai_memory")]
public class UpdateAiMemorySkill : BaseSkillImplementation
{
    private const string MemoryIdParameter = "memoryId";

    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly IEmbeddingService _embeddingService;

    public UpdateAiMemorySkill(
        IAgentMemoryRepository agentMemoryRepository,
        IEmbeddingService embeddingService)
    {
        _agentMemoryRepository = agentMemoryRepository;
        _embeddingService = embeddingService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var memoryId = GetRequiredGuid(parameters, MemoryIdParameter);

        var memory = await _agentMemoryRepository.GetByIdAsync(memoryId, cancellationToken);
        if (memory == null ||
            !AgentMemoryAccessPolicy.CanWrite(memory, context.UserId, context.UserPermissions.Contains(Roles.Admin)))
        {
            return SkillResult.Error($"Memory with ID '{memoryId}' not found.");
        }

        var key = GetParameter<string>(parameters, "key");
        var content = GetParameter<string>(parameters, "content");
        var category = GetParameter<string>(parameters, "category");
        var importance = GetParameter<int?>(parameters, "importance");
        var isPinned = GetParameter<bool?>(parameters, "isPinned");

        var contentChanged = false;

        if (!string.IsNullOrWhiteSpace(key))
        {
            memory.Key = key;
            contentChanged = true;
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            memory.Content = content;
            contentChanged = true;
        }

        if (!string.IsNullOrWhiteSpace(category))
            memory.Category = category;

        if (importance.HasValue)
            memory.Importance = Math.Clamp(importance.Value, 1, 10);

        if (isPinned.HasValue)
            memory.IsPinned = isPinned.Value;

        if (contentChanged)
        {
            memory.Embedding = await _embeddingService.GenerateEmbeddingAsync(
                $"{memory.Key}: {memory.Content}", cancellationToken);
        }

        await _agentMemoryRepository.UpdateAsync(memory, cancellationToken);

        return SkillResult.SuccessResult(
            new { memory.Id, memory.Key, memory.Content, memory.Category, memory.Importance, memory.IsPinned },
            $"Memory '{memory.Key}' updated.");
    }
}
