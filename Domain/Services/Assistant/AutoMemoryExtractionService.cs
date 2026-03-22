// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Analyzes user message and assistant response after each chat turn via separate LLM call
/// and stores extracted facts, preferences or decisions as AgentMemories.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public class AutoMemoryExtractionService : IAutoMemoryExtractionService
{
    private readonly ILogger<AutoMemoryExtractionService> _logger;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILLMRepository _llmRepository;
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly IEmbeddingService _embeddingService;

    private const float DuplicateSimilarityThreshold = 0.90f;
    private const int MaxExtractedMemoriesPerTurn = 3;
    private const int ExtractionMaxTokens = 512;
    private const double ExtractionTemperature = 0.1;
    private const int MinimumImportance = 5;

    private static readonly string ExtractionSystemPrompt =
        "You are a memory extraction assistant. Analyze the conversation turn and extract 0-" +
        MaxExtractedMemoriesPerTurn +
        " important facts worth remembering long-term. " +
        "Only extract: user preferences, decisions, company facts, procedural knowledge. " +
        "Ignore greetings, trivial questions, temporary requests. " +
        "Respond ONLY with a valid JSON array. Each element: {\"key\":string,\"content\":string,\"category\":string,\"importance\":number}. " +
        "Categories: fact, preference, decision, context, temporal, procedure, relationship, user_info, project_context, workflow. " +
        "Importance 1-10. If nothing worth remembering, return [].";

    public AutoMemoryExtractionService(
        ILogger<AutoMemoryExtractionService> logger,
        ILLMProviderFactory providerFactory,
        ILLMRepository llmRepository,
        IAgentMemoryRepository agentMemoryRepository,
        IEmbeddingService embeddingService)
    {
        _logger = logger;
        _providerFactory = providerFactory;
        _llmRepository = llmRepository;
        _agentMemoryRepository = agentMemoryRepository;
        _embeddingService = embeddingService;
    }

    public async Task ExtractAndStoreMemoriesAsync(
        Guid agentId,
        string userMessage,
        string assistantResponse,
        string userId)
    {
        try
        {
            var extractedFacts = await ExtractFactsFromConversationAsync(userMessage, assistantResponse);
            if (extractedFacts.Count == 0)
                return;

            foreach (var fact in extractedFacts)
            {
                await StoreIfNotDuplicateAsync(agentId, fact);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto memory extraction failed for agent {AgentId} — chat is unaffected", agentId);
        }
    }

    private async Task<List<ExtractedFact>> ExtractFactsFromConversationAsync(
        string userMessage,
        string assistantResponse)
    {
        var (model, provider) = await GetCheapestModelAndProviderAsync();
        if (model == null || provider == null)
        {
            _logger.LogDebug("No enabled LLM model/provider available for memory extraction");
            return [];
        }

        var conversationText = $"User: {userMessage}\nAssistant: {assistantResponse}";

        var request = new LLMProviderRequest
        {
            Message = conversationText,
            SystemPrompt = ExtractionSystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = ExtractionTemperature,
            MaxTokens = ExtractionMaxTokens,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken
        };

        var response = await provider.ProcessAsync(request);

        if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogDebug("Memory extraction LLM call returned no content");
            return [];
        }

        return ParseExtractedFacts(response.Content);
    }

    private List<ExtractedFact> ParseExtractedFacts(string jsonContent)
    {
        try
        {
            var cleaned = ExtractJsonArray(jsonContent);
            if (string.IsNullOrWhiteSpace(cleaned))
                return [];

            var items = JsonSerializer.Deserialize<List<JsonElement>>(cleaned);
            if (items == null || items.Count == 0)
                return [];

            var result = new List<ExtractedFact>();

            foreach (var item in items.Take(MaxExtractedMemoriesPerTurn))
            {
                if (!item.TryGetProperty("key", out var keyEl) ||
                    !item.TryGetProperty("content", out var contentEl))
                    continue;

                var key = keyEl.GetString();
                var content = contentEl.GetString();

                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(content))
                    continue;

                var category = item.TryGetProperty("category", out var catEl)
                    ? catEl.GetString() ?? MemoryCategories.LearnedFact
                    : MemoryCategories.LearnedFact;

                var importance = item.TryGetProperty("importance", out var impEl) && impEl.TryGetInt32(out var imp)
                    ? Math.Clamp(imp, 1, 10)
                    : MinimumImportance;

                result.Add(new ExtractedFact(key, content, category, importance));
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse extracted facts JSON");
            return [];
        }
    }

    private static string ExtractJsonArray(string content)
    {
        var start = content.IndexOf('[');
        var end = content.LastIndexOf(']');

        if (start < 0 || end < 0 || end <= start)
            return string.Empty;

        return content[start..(end + 1)];
    }

    private async Task StoreIfNotDuplicateAsync(Guid agentId, ExtractedFact fact)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync($"{fact.Key}: {fact.Content}");

        if (embedding != null)
        {
            var similar = await _agentMemoryRepository.HybridSearchAsync(
                agentId, $"{fact.Key} {fact.Content}", embedding, limit: 3);

            if (similar.Any(s => s.Score >= DuplicateSimilarityThreshold))
            {
                _logger.LogDebug("Skipping duplicate memory '{Key}' (similarity >= {Threshold})",
                    fact.Key, DuplicateSimilarityThreshold);
                return;
            }
        }

        var memory = new AgentMemory
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            Key = fact.Key,
            Content = fact.Content,
            Category = fact.Category,
            Importance = fact.Importance,
            Embedding = embedding,
            Source = MemorySources.Conversation,
            IsPinned = false
        };

        await _agentMemoryRepository.AddAsync(memory);

        _logger.LogDebug("Auto-extracted memory '{Key}' [{Category}] importance={Importance}",
            fact.Key, fact.Category, fact.Importance);
    }

    private async Task<(LLMModel? model, ILLMProvider? provider)> GetCheapestModelAndProviderAsync()
    {
        var models = await _llmRepository.GetModelsAsync(onlyEnabled: true);

        var cheapest = models
            .OrderBy(m => m.CostPerInputToken + m.CostPerOutputToken)
            .FirstOrDefault();

        if (cheapest == null)
            return (null, null);

        var provider = await _providerFactory.GetProviderForModelAsync(cheapest.ModelId);
        return (cheapest, provider);
    }

    private sealed record ExtractedFact(string Key, string Content, string Category, int Importance);
}
