// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Assembles the full LLM context per turn: cached identity (soul + rules),
/// sentiment mood hint, and relevant memories via hybrid search.
/// Parallelizes embedding generation (HTTP) with DB queries for lower latency.
/// </summary>
/// <param name="agentId">The agent whose soul/memory context to assemble</param>
/// <param name="userMessage">Current user message for sentiment + memory search</param>
/// <param name="language">UI language code for template variable resolution</param>

using System.Text;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Caching.Memory;

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
    private readonly ISentimentAnalyzer _sentimentAnalyzer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContextAssemblyPipeline> _logger;
    private readonly IMemoryCache _cache;

    private const int CharsPerToken = 4;
    private const int MaxMemoriesPerTurn = 15;
    private const int MaxPinnedMemories = 10;
    private const float SentimentThreshold = 0.5f;
    private const int IdentityCacheMinutes = 3;

    private static readonly Regex TemplateVariableRegex = new(
        @"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public ContextAssemblyPipeline(
        IAgentSoulRepository soulRepository,
        IAgentMemoryRepository memoryRepository,
        IAgentSkillRepository skillRepository,
        IGlobalAgentRuleRepository globalRuleRepository,
        IEmbeddingService embeddingService,
        ISentimentAnalyzer sentimentAnalyzer,
        IConfiguration configuration,
        ILogger<ContextAssemblyPipeline> logger,
        IMemoryCache cache)
    {
        _soulRepository = soulRepository;
        _memoryRepository = memoryRepository;
        _skillRepository = skillRepository;
        _globalRuleRepository = globalRuleRepository;
        _embeddingService = embeddingService;
        _sentimentAnalyzer = sentimentAnalyzer;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
    }

    public async Task<string> AssembleSoulAndMemoryPromptAsync(
        Guid agentId,
        string userMessage,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        var templateVariables = BuildTemplateVariables(language);

        var identityPrompt = await GetCachedIdentityPromptAsync(agentId, language, templateVariables, cancellationToken);
        sb.Append(identityPrompt);

        var sentimentResult = _sentimentAnalyzer.AnalyzeSentiment(userMessage);
        if (sentimentResult.Mood != SentimentMood.Neutral && sentimentResult.Confidence > SentimentThreshold)
        {
            sb.AppendLine($"[USER_MOOD: {sentimentResult.Mood.ToString().ToUpperInvariant()}] Adjust your tone accordingly.");
            sb.AppendLine();
        }

        var embeddingTask = _embeddingService.IsAvailable
            ? _embeddingService.GenerateEmbeddingAsync(userMessage, cancellationToken)
            : Task.FromResult<float[]?>(null);

        var pinnedMemories = await _memoryRepository.GetPinnedAsync(agentId, cancellationToken);

        var queryEmbedding = await embeddingTask;

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
        return text.Length / CharsPerToken;
    }

    private async Task<string> GetCachedIdentityPromptAsync(
        Guid agentId,
        string? language,
        Dictionary<string, string> templateVariables,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"identity_prompt_{agentId}_{language ?? "en"}";

        if (_cache.TryGetValue(cacheKey, out string? cached) && cached != null)
        {
            return cached;
        }

        var sb = new StringBuilder();

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

        var result = sb.ToString();

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(IdentityCacheMinutes)));

        return result;
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
