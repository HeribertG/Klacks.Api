// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class UpdateAiMemorySkill : BaseSkill
{
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly IEmbeddingService _embeddingService;

    public override string Name => "update_ai_memory";

    public override string Description =>
        "Updates an existing persistent memory entry. " +
        "You can update the key, content, category, importance, or pinned status. " +
        "If content changes, the embedding is regenerated automatically.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => [Roles.Admin];

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "memoryId",
            "The ID of the memory entry to update.",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "key",
            "New short identifier or title for the memory entry.",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "content",
            "New memory content.",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "category",
            "New category.",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "importance",
            "New importance level from 1 (low) to 10 (high).",
            SkillParameterType.Integer,
            Required: false),
        new SkillParameter(
            "isPinned",
            "Whether this memory should always be included in context.",
            SkillParameterType.Boolean,
            Required: false)
    };

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
