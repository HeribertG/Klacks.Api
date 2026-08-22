// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_ai_memories")]
public class GetAiMemoriesSkill : BaseSkillImplementation
{
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IEmbeddingService _embeddingService;

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

        var injectedIds = context.InjectedMemoryIds is { Count: > 0 }
            ? new HashSet<Guid>(context.InjectedMemoryIds)
            : null;

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(searchQuery, cancellationToken);
            var searchResults = await _agentMemoryRepository.HybridSearchAsync(
                agent.Id, searchQuery, queryEmbedding, 15, context.UserId, cancellationToken);

            var pinnedResults = await _agentMemoryRepository.GetPinnedAsync(agent.Id, context.UserId, cancellationToken);

            var combined = pinnedResults.Select(p => new
            {
                p.Id, p.Key, p.Content, p.Category, p.Importance,
                Score = 1.0f, IsPinned = true, CreatedAt = p.CreateTime
            }).Concat(searchResults.Select(r => new
            {
                r.Id, r.Key, r.Content, r.Category, r.Importance,
                r.Score, r.IsPinned, CreatedAt = (DateTime?)null
            })).DistinctBy(m => m.Id).ToList();

            var (filteredCombined, removedCombined) = FilterInjected(combined, m => m.Id, injectedIds);

            return SkillResult.SuccessResult(
                new { Memories = filteredCombined, Count = filteredCombined.Count, SearchType = "hybrid" },
                AppendAlreadyInContextNote($"Found {filteredCombined.Count} memories via hybrid search.", removedCombined));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var memories = await _agentMemoryRepository.SearchAsync(agent.Id, searchTerm, context.UserId, cancellationToken);
            var result = memories.Select(m => new { m.Id, m.Key, m.Content, m.Category, m.Importance, m.IsPinned, m.Source, CreatedAt = m.CreateTime }).ToList();
            var (filteredResult, removedResult) = FilterInjected(result, m => m.Id, injectedIds);

            return SkillResult.SuccessResult(
                new { Memories = filteredResult, Count = filteredResult.Count, SearchType = "text" },
                AppendAlreadyInContextNote($"Found {filteredResult.Count} memories via text search.", removedResult));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var memories = await _agentMemoryRepository.GetByCategoryAsync(agent.Id, category, context.UserId, cancellationToken);
            var result = memories.Select(m => new { m.Id, m.Key, m.Content, m.Category, m.Importance, m.IsPinned, m.Source, CreatedAt = m.CreateTime }).ToList();
            var (filteredResult, removedResult) = FilterInjected(result, m => m.Id, injectedIds);

            return SkillResult.SuccessResult(
                new { Memories = filteredResult, Count = filteredResult.Count },
                AppendAlreadyInContextNote($"Found {filteredResult.Count} memories in category '{category}'.", removedResult));
        }

        var allMemories = await _agentMemoryRepository.GetAllAsync(agent.Id, context.UserId, cancellationToken);

        var categories = allMemories
            .GroupBy(m => string.IsNullOrWhiteSpace(m.Category) ? "uncategorized" : m.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        var (sampleSource, removedFromSample) = FilterInjected(allMemories, m => m.Id, injectedIds);
        var sample = sampleSource
            .OrderByDescending(m => m.IsPinned)
            .ThenByDescending(m => m.Importance)
            .Take(OverviewSampleSize)
            .Select(m => new { m.Key, m.Category, m.Importance, m.IsPinned, Preview = Preview(m.Content) })
            .ToList();

        return SkillResult.SuccessResult(
            new
            {
                Count = allMemories.Count,
                Categories = categories,
                Sample = sample,
                Detail = "Compact overview only (previews are shortened). To read the full content of specific memories, call get_ai_memories again with searchQuery (semantic), searchTerm (keyword), or category."
            },
            AppendAlreadyInContextNote(
                $"{allMemories.Count} memories total across {categories.Count} categories. Returned a compact overview with the top {sample.Count} by importance. To read specific ones in full, call get_ai_memories with searchQuery, searchTerm or category.",
                removedFromSample));
    }

    private const int OverviewSampleSize = 25;
    private const int PreviewChars = 100;
    private const string AlreadyInContextFormat = "{0} additional matches are already in your context above.";

    private static string Preview(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        var single = content.ReplaceLineEndings(" ");
        return single.Length <= PreviewChars ? single : single[..PreviewChars] + "…";
    }

    private static (List<T> Filtered, int RemovedCount) FilterInjected<T>(
        IReadOnlyCollection<T> items, Func<T, Guid> idSelector, HashSet<Guid>? injectedIds)
    {
        if (injectedIds == null || injectedIds.Count == 0)
        {
            return (items.ToList(), 0);
        }

        var filtered = items.Where(item => !injectedIds.Contains(idSelector(item))).ToList();
        return (filtered, items.Count - filtered.Count);
    }

    private static string AppendAlreadyInContextNote(string message, int removedCount)
    {
        return removedCount > 0
            ? $"{message} {string.Format(AlreadyInContextFormat, removedCount)}"
            : message;
    }
}
