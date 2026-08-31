// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMConversationManager
{
    private readonly ILogger<LLMConversationManager> _logger;
    private readonly ILLMRepository _repository;

    // Scoped per request. Both the skill-retrieval query builder and the prompt preparation
    // load the same conversation history within one turn — memoize to save a duplicate DB read.
    private readonly Dictionary<string, List<Providers.LLMMessage>> _historyCache = new();

    private const string HistoryCacheKeySeparator = "|";

    public LLMConversationManager(
        ILogger<LLMConversationManager> logger,
        ILLMRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task<LLMConversation> GetOrCreateConversationAsync(string? conversationId, string userId)
    {
        return await _repository.GetOrCreateConversationAsync(
            conversationId ?? Guid.NewGuid().ToString(),
            userId);
    }

    public async Task<List<Providers.LLMMessage>> GetConversationHistoryAsync(string conversationId, string userId)
    {
        var cacheKey = BuildHistoryCacheKey(conversationId, userId);
        if (_historyCache.TryGetValue(cacheKey, out var cached))
        {
            return new List<Providers.LLMMessage>(cached);
        }

        var history = await _repository.GetConversationMessagesAsync(conversationId, userId);
        var mapped = history.Select(m => new Providers.LLMMessage
        {
            Role = m.Role,
            Content = ToolCallMarkupSanitizer.Sanitize(m.Content),
            Timestamp = m.CreateTime ?? DateTime.UtcNow
        }).ToList();

        _historyCache[cacheKey] = mapped;
        return new List<Providers.LLMMessage>(mapped);
    }

    private static string BuildHistoryCacheKey(string conversationId, string userId) =>
        $"{userId}{HistoryCacheKeySeparator}{conversationId}";

    public async Task SaveConversationMessagesAsync(
        LLMConversation conversation,
        string userMessage,
        string assistantMessage,
        string modelId)
    {
        await _repository.SaveMessageAsync(new LLMMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = userMessage,
            CreateTime = DateTime.UtcNow
        });

        if (ToolCallMarkupSanitizer.ContainsMarkup(assistantMessage))
        {
            _logger.LogWarning(
                "Assistant message for conversation {ConversationId} contained text-emitted tool-call markup " +
                "(no provider executes text tool calls). Stripping before persist to avoid few-shot contamination.",
                conversation.ConversationId);
        }

        await _repository.SaveMessageAsync(new LLMMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = ToolCallMarkupSanitizer.Sanitize(assistantMessage),
            ModelId = modelId,
            CreateTime = DateTime.UtcNow
        });

        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.MessageCount += 2;
        conversation.LastModelId = modelId;

        if (conversation.MessageCount == 2)
        {
            conversation.Title = GenerateConversationTitle(userMessage, assistantMessage);
        }

        await _repository.UpdateConversationAsync(conversation);
        _historyCache.Remove(BuildHistoryCacheKey(conversation.ConversationId, conversation.UserId));
    }

    public async Task TrackUsageAsync(
        string userId,
        LLMModel model,
        LLMConversation conversation,
        Providers.LLMUsage usage,
        long responseTimeMs,
        bool hasError = false,
        string? errorMessage = null,
        long? ttftMs = null,
        long? toolsetAssemblyMs = null,
        int? toolIterations = null,
        Guid? turnId = null,
        string? functionsCalledJson = null)
    {
        await _repository.TrackUsageAsync(new LLMUsage
        {
            // W1.1: the usage row carries the turn id as its primary key, making
            // llm_usages.id == skill_selection_trajectories.turn_id == skill_usage_records.turn_id.
            Id = turnId ?? Guid.NewGuid(),
            UserId = userId,
            ModelId = model.Id,
            ConversationId = conversation.ConversationId,
            InputTokens = usage.InputTokens,
            OutputTokens = usage.OutputTokens,
            CacheCreationInputTokens = usage.CacheCreationInputTokens,
            CacheReadInputTokens = usage.CacheReadInputTokens,
            Cost = usage.Cost,
            ResponseTimeMs = (int)responseTimeMs,
            HasError = hasError,
            ErrorMessage = errorMessage,
            FunctionsCalled = functionsCalledJson,
            TtftMs = ttftMs.HasValue ? (int)Math.Min(ttftMs.Value, int.MaxValue) : null,
            ToolsetAssemblyMs = toolsetAssemblyMs.HasValue ? (int)Math.Min(toolsetAssemblyMs.Value, int.MaxValue) : null,
            ToolIterations = toolIterations,
            CreateTime = DateTime.UtcNow
        });

        if (!hasError)
        {
            conversation.TotalTokens += usage.TotalTokens;
            conversation.TotalCost += usage.Cost;
        }
    }

    private string GenerateConversationTitle(string userMessage, string assistantMessage)
    {
        var words = userMessage.Split(' ').Take(5);
        return string.Join(' ', words) + (words.Count() >= 5 ? "..." : "");
    }
}
