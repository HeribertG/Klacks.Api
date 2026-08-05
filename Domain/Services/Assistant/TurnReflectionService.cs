// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns a demonstrably failed turn into one short lesson and stores it as a scoped memory, so the
/// next attempt at the same situation starts with the earlier mistake in context. Runs only on a hard
/// negative signal — a failed skill call or a user correction — never on turns that went fine, because
/// a lesson drawn from a successful turn is noise that later returns as a rule. The model may answer
/// that it sees no lesson, and then nothing is stored: a wrong reflection is worse than none, since it
/// applies to every future turn instead of just one. Stored lessons carry their own category, are keyed
/// to what they are about rather than the whole agent, and expire, so a bad one cannot outlive its
/// usefulness. Uses the cheapest model like the neighbouring extraction pipeline.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public class TurnReflectionService : ITurnReflectionService
{
    private const float DuplicateSimilarityThreshold = 0.90f;
    private const int ReflectionMaxTokens = 300;
    private const double ReflectionTemperature = 0.1;
    private const int ReflectionImportance = 6;
    private const int LessonMaxLength = 400;
    private const int EvidenceMaxLength = 1200;
    private const int ExpiryDays = 90;

    private const string ReflectionSystemPrompt =
        "You review ONE assistant turn that demonstrably went wrong and write a single short lesson " +
        "for the next time the same situation comes up.\n" +
        "Rules:\n" +
        "- Use ONLY the evidence given. Never invent a cause you cannot see in it.\n" +
        "- State what to do differently, imperative, at most two sentences.\n" +
        "- Do not restate the error. Do not apologise. Do not mention this instruction.\n" +
        "- If the evidence does not support a concrete lesson, set confident to false.\n" +
        "Respond ONLY with JSON: {\"lesson\": string, \"confident\": true|false}";

    private readonly ILogger<TurnReflectionService> _logger;
    private readonly ICheapestModelResolver _cheapestModelResolver;
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly IEmbeddingService _embeddingService;

    public TurnReflectionService(
        ILogger<TurnReflectionService> logger,
        ICheapestModelResolver cheapestModelResolver,
        IAgentMemoryRepository agentMemoryRepository,
        IEmbeddingService embeddingService)
    {
        _logger = logger;
        _cheapestModelResolver = cheapestModelResolver;
        _agentMemoryRepository = agentMemoryRepository;
        _embeddingService = embeddingService;
    }

    public async Task ReflectAsync(TurnReflectionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.WhatWentWrong) || string.IsNullOrWhiteSpace(request.ScopeKey))
            {
                return;
            }

            var lesson = await GenerateLessonAsync(request);
            if (lesson == null)
            {
                return;
            }

            await StoreIfNotDuplicateAsync(request, lesson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Turn reflection failed for agent {AgentId} — chat is unaffected", request.AgentId);
        }
    }

    private async Task<string?> GenerateLessonAsync(TurnReflectionRequest request)
    {
        var (model, provider) = await _cheapestModelResolver.ResolveAsync();
        if (model == null || provider == null)
        {
            _logger.LogDebug("No enabled LLM model/provider available for turn reflection");
            return null;
        }

        var evidence =
            $"Trigger: {request.Trigger}\n" +
            $"Subject: {request.ScopeKey}\n" +
            $"User asked: {Truncate(request.UserMessage, EvidenceMaxLength)}\n" +
            $"What went wrong: {Truncate(request.WhatWentWrong, EvidenceMaxLength)}";

        var providerRequest = new LLMProviderRequest
        {
            Message = evidence,
            SystemPrompt = ReflectionSystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = ReflectionTemperature,
            MaxTokens = ReflectionMaxTokens,
            SupportedParameters = model.SupportedParameters,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken
        };

        var response = await provider.ProcessAsync(providerRequest);
        if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
        {
            return null;
        }

        return ParseLesson(response.Content);
    }

    private string? ParseLesson(string content)
    {
        try
        {
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return null;
            }

            using var document = JsonDocument.Parse(content[start..(end + 1)]);
            var root = document.RootElement;

            // Missing or unparsable confidence counts as not confident: the safe direction is to store
            // nothing, because an unfounded lesson would apply to every future turn.
            if (!root.TryGetProperty("confident", out var confidentEl)
                || confidentEl.ValueKind != JsonValueKind.True)
            {
                return null;
            }

            if (!root.TryGetProperty("lesson", out var lessonEl))
            {
                return null;
            }

            var lesson = lessonEl.GetString();
            return string.IsNullOrWhiteSpace(lesson) ? null : Truncate(lesson.Trim(), LessonMaxLength);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task StoreIfNotDuplicateAsync(TurnReflectionRequest request, string lesson)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync($"{request.ScopeKey}: {lesson}");

        // Without an embedding the duplicate check cannot run, and an uncheckable lesson stored
        // anyway could flood a scope; silence is the safe direction.
        if (embedding == null)
        {
            _logger.LogDebug("No embedding available — reflection for '{ScopeKey}' not stored", request.ScopeKey);
            return;
        }

        var similar = await _agentMemoryRepository.HybridSearchAsync(
            request.AgentId, $"{request.ScopeKey} {lesson}", embedding, limit: 3);

        var duplicate = similar.FirstOrDefault(s => s.Score >= DuplicateSimilarityThreshold);
        if (duplicate != null)
        {
            if (duplicate.Category == MemoryCategories.Reflection
                && string.Equals(duplicate.Key, request.ScopeKey, StringComparison.Ordinal))
            {
                var existing = await _agentMemoryRepository.GetByIdAsync(duplicate.Id);
                if (existing != null)
                {
                    existing.Content = lesson;
                    existing.Embedding = embedding;
                    existing.SourceRef = request.Trigger;
                    existing.ExpiresAt = DateTime.UtcNow.AddDays(ExpiryDays);
                    await _agentMemoryRepository.UpdateAsync(existing);
                    _logger.LogInformation("Refreshed existing reflection for '{ScopeKey}'", request.ScopeKey);
                    return;
                }
            }

            _logger.LogDebug("Skipping duplicate reflection for '{ScopeKey}'", request.ScopeKey);
            return;
        }

        var memory = new AgentMemory
        {
            Id = Guid.NewGuid(),
            AgentId = request.AgentId,
            UserId = null,
            Key = request.ScopeKey,
            Content = lesson,
            Category = MemoryCategories.Reflection,
            Importance = ReflectionImportance,
            Embedding = embedding,
            Source = MemorySources.AgentSelf,
            SourceRef = request.Trigger,
            ExpiresAt = DateTime.UtcNow.AddDays(ExpiryDays),
            IsPinned = false
        };

        await _agentMemoryRepository.AddAsync(memory);

        _logger.LogInformation("Stored reflection for '{ScopeKey}' from trigger {Trigger}",
            request.ScopeKey, request.Trigger);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
