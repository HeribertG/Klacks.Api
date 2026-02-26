// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetAiMemoriesSkill : BaseSkill
{
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IEmbeddingService _embeddingService;

    public override string Name => "get_ai_memories";

    public override string Description =>
        "Retrieves the AI assistant's persistent memory entries. " +
        "Supports semantic hybrid search (vector + full-text + importance scoring). " +
        "Can also filter by category.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => [Roles.Admin];

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "searchQuery",
            "Semantic search query - uses hybrid search (vector similarity + full-text + importance).",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "category",
            "Filter by category: fact, preference, decision, user_info, project_context, learned_behavior, correction, temporal.",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "searchTerm",
            "Simple text search in key and content (fallback if no searchQuery).",
            SkillParameterType.String,
            Required: false)
    };

    public GetAiMemoriesSkill(
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
        var searchQuery = GetParameter<string>(parameters, "searchQuery");
        var category = GetParameter<string>(parameters, "category");
        var searchTerm = GetParameter<string>(parameters, "searchTerm");

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.SuccessResult(new { Memories = Array.Empty<object>(), Count = 0 }, "No agent configured.");
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(searchQuery, cancellationToken);
            var searchResults = await _agentMemoryRepository.HybridSearchAsync(
                agent.Id, searchQuery, queryEmbedding, 15, cancellationToken);

            var pinnedResults = await _agentMemoryRepository.GetPinnedAsync(agent.Id, cancellationToken);

            var combined = pinnedResults.Select(p => new
            {
                p.Id, p.Key, p.Content, p.Category, p.Importance,
                Score = 1.0f, IsPinned = true, CreatedAt = p.CreateTime
            }).Concat(searchResults.Select(r => new
            {
                r.Id, r.Key, r.Content, r.Category, r.Importance,
                r.Score, r.IsPinned, CreatedAt = (DateTime?)null
            })).DistinctBy(m => m.Id).ToList();

            return SkillResult.SuccessResult(
                new { Memories = combined, Count = combined.Count, SearchType = "hybrid" },
                $"Found {combined.Count} memories via hybrid search.");
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var memories = await _agentMemoryRepository.SearchAsync(agent.Id, searchTerm, cancellationToken);
            var result = memories.Select(m => new { m.Id, m.Key, m.Content, m.Category, m.Importance, m.IsPinned, m.Source, CreatedAt = m.CreateTime }).ToList();
            return SkillResult.SuccessResult(
                new { Memories = result, Count = result.Count, SearchType = "text" },
                $"Found {result.Count} memories via text search.");
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var memories = await _agentMemoryRepository.GetByCategoryAsync(agent.Id, category, cancellationToken);
            var result = memories.Select(m => new { m.Id, m.Key, m.Content, m.Category, m.Importance, m.IsPinned, m.Source, CreatedAt = m.CreateTime }).ToList();
            return SkillResult.SuccessResult(
                new { Memories = result, Count = result.Count },
                $"Found {result.Count} memories in category '{category}'.");
        }

        var allMemories = await _agentMemoryRepository.GetAllAsync(agent.Id, cancellationToken);
        var allResult = allMemories.Select(m => new { m.Id, m.Key, m.Content, m.Category, m.Importance, m.IsPinned, m.Source, CreatedAt = m.CreateTime }).ToList();

        return SkillResult.SuccessResult(
            new { Memories = allResult, Count = allResult.Count },
            $"Found {allResult.Count} memory entries.");
    }
}
