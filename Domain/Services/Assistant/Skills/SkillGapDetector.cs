// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Analyses each completed chat turn for signs of an unfulfilled user request and records
/// them as SkillGapRecords. Designed for fire-and-forget invocation from LLMService.
/// </summary>
/// <param name="agentId">ID of the agent that handled the turn</param>
/// <param name="userMessage">Raw user message from the turn</param>
/// <param name="assistantResponse">Raw assistant response from the turn</param>
/// <param name="hadFunctionCalls">Whether the LLM invoked any skill/function during the turn</param>

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public class SkillGapDetector : ISkillGapDetector
{
    private readonly ISkillGapRepository _repository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<SkillGapDetector> _logger;

    private const float SimilarityThreshold = 0.85f;
    private const int MaxUserMessageLength = 1000;

    private static readonly string[] GapIndicatorPhrases =
    [
        "kann ich leider nicht",
        "diese funktion gibt es nicht",
        "habe ich keinen zugriff",
        "ist nicht möglich",
        "bin leider nicht in der lage",
        "diese fähigkeit habe ich nicht",
        "dafür habe ich keine",
        "i cannot do that",
        "i don't have the ability",
        "i'm unable to",
        "i do not have access",
        "that functionality is not available",
        "i lack the capability",
        "no skill available",
        "this is not supported",
        "i cannot perform",
        "unfortunately i cannot",
        "i am not able to",
        "je ne peux pas",
        "je n'ai pas accès",
        "ce n'est pas possible",
        "cette fonction n'existe pas",
        "non posso",
        "non ho accesso",
        "non è possibile",
        "questa funzione non esiste"
    ];

    private static readonly object _configureLock = new();
    private static string[] _pluginGapIndicatorPhrases = [];

    /// <summary>
    /// Extends gap detection with plugin language phrases. Called once at startup by
    /// ConversationSignalsPluginLoader after reading conversation-signals.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> gapIndicatorPhrases)
    {
        lock (_configureLock)
        {
            _pluginGapIndicatorPhrases = PluginPhraseMatcher.Merge(_pluginGapIndicatorPhrases, gapIndicatorPhrases);
        }
    }

    public SkillGapDetector(
        ISkillGapRepository repository,
        IEmbeddingService embeddingService,
        ILogger<SkillGapDetector> logger)
    {
        _repository = repository;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task DetectAndSuggestAsync(
        Guid agentId,
        string userMessage,
        string assistantResponse,
        bool hadFunctionCalls)
    {
        if (hadFunctionCalls)
        {
            return;
        }

        if (AffirmationDetector.IsAffirmation(userMessage))
        {
            return;
        }

        if (!ContainsGapIndicator(assistantResponse))
        {
            return;
        }

        try
        {
            var truncatedMessage = userMessage.Length > MaxUserMessageLength
                ? userMessage[..MaxUserMessageLength]
                : userMessage;

            var normalizedHash = HashNormalized(truncatedMessage);

            var existingByHash = await _repository.FindByNormalizedHashAsync(agentId, normalizedHash);
            if (existingByHash != null)
            {
                existingByHash.OccurrenceCount++;
                existingByHash.LastDetectedAt = DateTime.UtcNow;
                existingByHash.UpdateTime = DateTime.UtcNow;
                await _repository.UpdateAsync(existingByHash);
                return;
            }

            var embedding = await _embeddingService.GenerateEmbeddingAsync(truncatedMessage);

            if (embedding != null && embedding.Length > 0)
            {
                var existingByEmbedding = await _repository.FindSimilarAsync(agentId, embedding, SimilarityThreshold);
                if (existingByEmbedding != null)
                {
                    existingByEmbedding.OccurrenceCount++;
                    existingByEmbedding.LastDetectedAt = DateTime.UtcNow;
                    existingByEmbedding.UpdateTime = DateTime.UtcNow;
                    await _repository.UpdateAsync(existingByEmbedding);
                    return;
                }
            }

            var record = new SkillGapRecord
            {
                Id = Guid.NewGuid(),
                AgentId = agentId,
                UserMessage = truncatedMessage,
                DetectedIntent = ExtractIntent(truncatedMessage),
                OccurrenceCount = 1,
                Status = SkillGapStatuses.Detected,
                FirstDetectedAt = DateTime.UtcNow,
                LastDetectedAt = DateTime.UtcNow,
                Embedding = embedding,
                NormalizedMessageHash = normalizedHash,
                CreateTime = DateTime.UtcNow
            };

            await _repository.AddAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Skill gap detection failed for agent {AgentId}", agentId);
        }
    }

    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private static string HashNormalized(string message)
    {
        var normalized = WhitespaceRun.Replace(message.Trim(), " ").ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    private static bool ContainsGapIndicator(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        var lower = response.ToLowerInvariant();
        if (GapIndicatorPhrases.Any(phrase => lower.Contains(phrase)))
        {
            return true;
        }

        var tokens = WordPattern.Matches(response)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();
        return PluginPhraseMatcher.MatchesAny(lower, tokens, _pluginGapIndicatorPhrases);
    }

    private static string ExtractIntent(string message)
    {
        if (message.Length <= 120)
        {
            return message;
        }

        var firstSentenceEnd = message.IndexOfAny(['.', '!', '?', '\n']);
        if (firstSentenceEnd > 0 && firstSentenceEnd <= 200)
        {
            return message[..firstSentenceEnd].Trim();
        }

        return message[..120].Trim() + "...";
    }
}
