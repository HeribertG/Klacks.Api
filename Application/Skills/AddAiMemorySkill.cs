using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class AddAiMemorySkill : BaseSkill
{
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IEmbeddingService _embeddingService;

    public override string Name => "add_ai_memory";

    public override string Description =>
        "Adds a new persistent memory entry for the AI assistant. " +
        "Memories persist across all conversations and help the assistant remember important facts, " +
        "user preferences, and system knowledge. Embeddings are generated automatically for semantic search.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "Admin" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "key",
            "Short identifier or title for the memory entry.",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "content",
            "The actual memory content to remember.",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "category",
            "Category: fact, preference, decision, user_info, project_context, learned_behavior, correction, temporal, user_preference, system_knowledge, learned_fact, workflow, context.",
            SkillParameterType.String,
            Required: false,
            DefaultValue: MemoryCategories.LearnedFact),
        new SkillParameter(
            "importance",
            "Importance level from 1 (low) to 10 (high).",
            SkillParameterType.Integer,
            Required: false,
            DefaultValue: 5),
        new SkillParameter(
            "isPinned",
            "If true, this memory is always included in the context.",
            SkillParameterType.Boolean,
            Required: false,
            DefaultValue: false),
        new SkillParameter(
            "tags",
            "Comma-separated tags for categorization.",
            SkillParameterType.String,
            Required: false)
    };

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
