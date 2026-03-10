// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_ai_memory")]
public class AddAiMemorySkill : BaseSkillImplementation
{
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IEmbeddingService _embeddingService;

    public AddAiMemorySkill(
        IAgentMemoryRepository agentMemoryRepository,
        IAgentRepository agentRepository,
        IEmbeddingService embeddingService)
    {
        _agentMemoryRepository = agentMemoryRepository;
        _agentRepository = agentRepository;
        _embeddingService = embeddingService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var key = GetRequiredString(parameters, "key");
        var content = GetRequiredString(parameters, "content");
        var category = GetParameter<string>(parameters, "category", MemoryCategories.LearnedFact)!;
        var importance = GetParameter<int?>(parameters, "importance", 5) ?? 5;
        var isPinned = GetParameter<bool?>(parameters, "isPinned") ?? false;
        var tagsStr = GetParameter<string>(parameters, "tags");

        importance = Math.Clamp(importance, 1, 10);

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.Error("No agent is configured yet.");
        }

        var embedding = await _embeddingService.GenerateEmbeddingAsync($"{key}: {content}", cancellationToken);

        var memory = new AgentMemory
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            Key = key,
            Content = content,
            Category = category,
            Importance = importance,
            IsPinned = isPinned,
            Embedding = embedding,
            Source = MemorySources.Chat
        };

        if (!string.IsNullOrWhiteSpace(tagsStr))
        {
            var tags = tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var tag in tags)
            {
                memory.Tags.Add(new AgentMemoryTag { MemoryId = memory.Id, Tag = tag });
            }
        }

        await _agentMemoryRepository.AddAsync(memory, cancellationToken);

        return SkillResult.SuccessResult(
            new { MemoryId = memory.Id, Key = key, Category = category, Importance = importance, HasEmbedding = embedding != null, IsPinned = isPinned },
            $"Memory added: '{key}' [{category}] (importance: {importance}{(isPinned ? ", pinned" : "")}{(embedding != null ? ", with embedding" : ", embedding pending")}).");
    }
}
