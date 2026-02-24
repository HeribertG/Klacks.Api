// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text;
using System.Text.RegularExpressions;
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
    private readonly IGlobalAgentRuleRepository _globalRuleRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContextAssemblyPipeline> _logger;

    private const int TokensPerChar = 4;
    private const int MaxMemoriesPerTurn = 15;
    private const int MaxPinnedMemories = 10;

    private static readonly Regex TemplateVariableRegex = new(
        @"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public ContextAssemblyPipeline(
        IAgentSoulRepository soulRepository,
        IAgentMemoryRepository memoryRepository,
        IAgentSkillRepository skillRepository,
        IGlobalAgentRuleRepository globalRuleRepository,
        IEmbeddingService embeddingService,
        IConfiguration configuration,
        ILogger<ContextAssemblyPipeline> logger)
    {
        _soulRepository = soulRepository;
        _memoryRepository = memoryRepository;
        _skillRepository = skillRepository;
        _globalRuleRepository = globalRuleRepository;
        _embeddingService = embeddingService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> AssembleSoulAndMemoryPromptAsync(
        Guid agentId,
        string userMessage,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        var templateVariables = BuildTemplateVariables(language);

        var globalRules = await _globalRuleRepository.GetActiveRulesAsync(cancellationToken);
        if (globalRules.Count > 0)
        {
            sb.AppendLine("=== GLOBAL RULES ===");
            foreach (var rule in globalRules)
            {
                sb.AppendLine($"[{rule.Name.ToUpperInvariant()}]");
                sb.AppendLine(ResolveTemplateVariables(rule.Content.Trim(), templateVariables));
                sb.AppendLine();
            }
            sb.AppendLine("====================");
            sb.AppendLine();
        }

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

    private Dictionary<string, string> BuildTemplateVariables(string? language)
    {
        var langCode = language ?? "en";
        var langName = _configuration[$"Languages:Metadata:{langCode}:Name"];
        var langDisplayName = _configuration[$"Languages:Metadata:{langCode}:DisplayName"];
        var langLabel = !string.IsNullOrEmpty(langName) && !string.IsNullOrEmpty(langDisplayName)
            ? $"{langName} ({langDisplayName})"
            : langCode;

        return new Dictionary<string, string>
        {
            ["LANGUAGE"] = langLabel,
            ["LANGUAGE_CODE"] = langCode,
        };
    }

    private static string ResolveTemplateVariables(
        string content, Dictionary<string, string> variables)
    {
        return TemplateVariableRegex.Replace(content, match =>
        {
            var key = match.Groups[1].Value;
            return variables.GetValueOrDefault(key, match.Value);
        });
    }
}
