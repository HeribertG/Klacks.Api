// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Assembles the full LLM context per turn: cached identity (soul + rules),
/// sentiment mood hint, and relevant memories via hybrid search.
/// </summary>
/// <param name="identityContextProvider">Provides cached identity prompt for the agent</param>
/// <param name="memoryRetrievalService">Retrieves relevant memories for the user message</param>
/// <param name="sentimentAnalyzer">Analyzes user message sentiment for mood hints</param>
/// <param name="logger">Logger instance</param>

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
    private readonly IIdentityContextProvider _identityContextProvider;
    private readonly IMemoryRetrievalService _memoryRetrievalService;
    private readonly ISentimentAnalyzer _sentimentAnalyzer;
    private readonly ILogger<ContextAssemblyPipeline> _logger;

    private const int CharsPerToken = 4;
    private const float SentimentThreshold = 0.5f;

    public ContextAssemblyPipeline(
        IIdentityContextProvider identityContextProvider,
        IMemoryRetrievalService memoryRetrievalService,
        ISentimentAnalyzer sentimentAnalyzer,
        ILogger<ContextAssemblyPipeline> logger)
    {
        _identityContextProvider = identityContextProvider;
        _memoryRetrievalService = memoryRetrievalService;
        _sentimentAnalyzer = sentimentAnalyzer;
        _logger = logger;
    }

    public async Task<string> AssembleSoulAndMemoryPromptAsync(
        Guid agentId,
        string userMessage,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        var identityPrompt = await _identityContextProvider.GetIdentityPromptAsync(agentId, language, cancellationToken);
        sb.Append(identityPrompt);

        var sentimentResult = await _sentimentAnalyzer.AnalyzeSentimentAsync(userMessage);
        if (sentimentResult.Mood != SentimentMood.Neutral && sentimentResult.Confidence > SentimentThreshold)
        {
            sb.AppendLine($"[USER_MOOD: {sentimentResult.Mood.ToString().ToUpperInvariant()}] Adjust your tone accordingly.");
            sb.AppendLine();
        }

        var memoryBlock = await _memoryRetrievalService.RetrieveRelevantMemoriesAsync(agentId, userMessage, cancellationToken);
        sb.Append(memoryBlock);

        return sb.ToString();
    }

    public int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Length / CharsPerToken;
    }
}
