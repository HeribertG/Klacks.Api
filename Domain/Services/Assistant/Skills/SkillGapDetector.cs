// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Analyses each completed chat turn for signs of an unfulfilled user request and records
/// them as SkillGapRecords. Designed for fire-and-forget invocation from LLMService.
/// </summary>
/// <param name="agentId">ID of the agent that handled the turn</param>
/// <param name="userMessage">Raw user message from the turn</param>
/// <param name="assistantResponse">Raw assistant response from the turn</param>
/// <param name="hadFunctionCalls">Whether the LLM invoked any skill/function during the turn</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

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
        "i am not able to"
    ];

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

        if (!ContainsGapIndicator(assistantResponse))
        {
            return;
        }

        try
        {
            var truncatedMessage = userMessage.Length > MaxUserMessageLength
                ? userMessage[..MaxUserMessageLength]
                : userMessage;

            var embedding = await _embeddingService.GenerateEmbeddingAsync(truncatedMessage);

            if (embedding != null && embedding.Length > 0)
            {
                var existing = await _repository.FindSimilarAsync(agentId, embedding, SimilarityThreshold);
                if (existing != null)
                {
                    existing.OccurrenceCount++;
                    existing.LastDetectedAt = DateTime.UtcNow;
                    existing.UpdateTime = DateTime.UtcNow;
                    await _repository.UpdateAsync(existing);
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
                CreateTime = DateTime.UtcNow
            };

            await _repository.AddAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Skill gap detection failed for agent {AgentId}", agentId);
        }
    }

    private static bool ContainsGapIndicator(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        var lower = response.ToLowerInvariant();
        return GapIndicatorPhrases.Any(phrase => lower.Contains(phrase));
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
