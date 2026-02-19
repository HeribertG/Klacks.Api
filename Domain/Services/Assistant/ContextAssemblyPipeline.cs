using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public record AssembledContext(
    string SystemPrompt,
    List<AgentSessionMessage> MessageHistory,
    List<AgentSkill> ActiveSkills,
    int TotalTokenEstimate);

public class ContextAssemblyPipeline
{
    private readonly IAgentSoulRepository _soulRepository;
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IAgentSkillRepository _skillRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<ContextAssemblyPipeline> _logger;

    private const int TokensPerChar = 4;
    private const int MaxMemoriesPerTurn = 15;
    private const int MaxPinnedMemories = 10;

    public ContextAssemblyPipeline(
        IAgentSoulRepository soulRepository,
        IAgentMemoryRepository memoryRepository,
        IAgentSkillRepository skillRepository,
        IEmbeddingService embeddingService,
        ILogger<ContextAssemblyPipeline> logger)
    {
        _soulRepository = soulRepository;
        _memoryRepository = memoryRepository;
        _skillRepository = skillRepository;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<string> AssembleSoulAndMemoryPromptAsync(
        Guid agentId,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        var sections = await _soulRepository.GetActiveSectionsAsync(agentId, cancellationToken);
        if (sections.Count > 0)
        {
            sb.AppendLine("=== IDENTITY ===");
            foreach (var section in sections)
            {
                sb.AppendLine($"[{section.SectionType.ToUpperInvariant()}]");
                sb.AppendLine(section.Content.Trim());
                sb.AppendLine();
            }
            sb.AppendLine("================");
            sb.AppendLine();
        }

        var pinnedMemories = await _memoryRepository.GetPinnedAsync(agentId, cancellationToken);
        var queryEmbedding = _embeddingService.IsAvailable
            ? await _embeddingService.GenerateEmbeddingAsync(userMessage, cancellationToken)
            : null;

        var searchResults = await _memoryRepository.HybridSearchAsync(
            agentId, userMessage, queryEmbedding, MaxMemoriesPerTurn, cancellationToken);

        var allMemoryIds = searchResults.Select(r => r.Id).ToList();
        if (allMemoryIds.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                try { await _memoryRepository.UpdateAccessCountsAsync(allMemoryIds); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to update memory access counts"); }
            }, CancellationToken.None);
        }

        var hasMemories = pinnedMemories.Count > 0 || searchResults.Count > 0;
        if (hasMemories)
        {
            sb.AppendLine();
            sb.AppendLine("=== PERSISTENT KNOWLEDGE ===");

            if (pinnedMemories.Count > 0)
            {
                sb.AppendLine("[PINNED]");
                foreach (var m in pinnedMemories.Take(MaxPinnedMemories))
                {
                    sb.AppendLine($"- [{m.Category}] {m.Key}: {m.Content}");
                }
            }

            if (searchResults.Count > 0)
            {
                sb.AppendLine("[RELEVANT]");
                foreach (var m in searchResults)
                {
                    sb.AppendLine($"- [{m.Category}] {m.Key}: {m.Content}");
                }
            }

            sb.AppendLine("============================");
        }

        return sb.ToString();
    }

    public int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Length / TokensPerChar;
    }
}
