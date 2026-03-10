// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_ai_memory")]
public class UpdateAiMemorySkill : BaseSkillImplementation
{
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
        var memoryId = GetRequiredGuid(parameters, "memoryId");

        var memory = await _agentMemoryRepository.GetByIdAsync(memoryId, cancellationToken);
        if (memory == null)
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
