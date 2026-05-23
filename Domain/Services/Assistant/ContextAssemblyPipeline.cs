// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Assembles the full LLM context per turn: cached identity (soul + rules),
/// world-model ontology block, sentiment mood hint, and relevant memories via hybrid search.
/// </summary>
/// <param name="identityContextProvider">Provides cached identity prompt for the agent</param>
/// <param name="ontologyService">Provides the Klacks domain ontology (entities, relations, constraints)</param>
/// <param name="memoryRetrievalService">Retrieves relevant memories for the user message</param>
/// <param name="sentimentAnalyzer">Analyzes user message sentiment for mood hints</param>
/// <param name="logger">Logger instance</param>

using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class ContextAssemblyPipeline
{
    private readonly IIdentityContextProvider _identityContextProvider;
    private readonly IKlacksOntologyService _ontologyService;
    private readonly IMemoryRetrievalService _memoryRetrievalService;
    private readonly ISentimentAnalyzer _sentimentAnalyzer;
    private readonly ILogger<ContextAssemblyPipeline> _logger;

    private const int CharsPerToken = 4;
    private const float SentimentThreshold = 0.5f;
    private const int OntologyBlockMaxTokens = 1500;

    // Below this length, an utterance is too short for sentiment/memory retrieval
    // to add value — typical examples are "ja", "ok", "weiter", "?". Skipping
    // saves one sentiment call + one embedding round-trip per such turn.
    private const int MinLengthForSemanticEnrichment = 8;

    public ContextAssemblyPipeline(
        IIdentityContextProvider identityContextProvider,
        IKlacksOntologyService ontologyService,
        IMemoryRetrievalService memoryRetrievalService,
        ISentimentAnalyzer sentimentAnalyzer,
        ILogger<ContextAssemblyPipeline> logger)
    {
        _identityContextProvider = identityContextProvider;
        _ontologyService = ontologyService;
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

        var ontologyBlock = _ontologyService.RenderWorldModelBlock(OntologyBlockMaxTokens);
        if (!string.IsNullOrWhiteSpace(ontologyBlock))
        {
            sb.AppendLine();
            sb.AppendLine(ontologyBlock);
            sb.AppendLine();
        }

        if ((userMessage?.Trim().Length ?? 0) < MinLengthForSemanticEnrichment)
        {
            _logger.LogDebug("Skipping sentiment + memory retrieval for short utterance (len < {Min})", MinLengthForSemanticEnrichment);
            return sb.ToString();
        }

        var sentimentTask = _sentimentAnalyzer.AnalyzeSentimentAsync(userMessage!);
        var memoryTask = _memoryRetrievalService.RetrieveRelevantMemoriesAsync(agentId, userMessage!, cancellationToken);

        await Task.WhenAll(sentimentTask, memoryTask);

        var sentimentResult = sentimentTask.Result;
        if (sentimentResult.Mood != SentimentMood.Neutral && sentimentResult.Confidence > SentimentThreshold)
        {
            sb.AppendLine($"[USER_MOOD: {sentimentResult.Mood.ToString().ToUpperInvariant()}] Adjust your tone accordingly.");
            sb.AppendLine();
        }

        sb.Append(memoryTask.Result);

        return sb.ToString();
    }

    public int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Length / CharsPerToken;
    }
}
