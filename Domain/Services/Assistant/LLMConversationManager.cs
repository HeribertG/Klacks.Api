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

    public async Task<List<Providers.LLMMessage>> GetConversationHistoryAsync(string conversationId)
    {
        if (_historyCache.TryGetValue(conversationId, out var cached))
        {
            return new List<Providers.LLMMessage>(cached);
        }

        var history = await _repository.GetConversationMessagesAsync(conversationId);
        var mapped = history.Select(m => new Providers.LLMMessage
        {
            Role = m.Role,
            Content = ToolCallMarkupSanitizer.Sanitize(m.Content),
            Timestamp = m.CreateTime ?? DateTime.UtcNow
        }).ToList();

        _historyCache[conversationId] = mapped;
        return new List<Providers.LLMMessage>(mapped);
    }

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
        _historyCache.Remove(conversation.ConversationId);
    }

    public async Task TrackUsageAsync(
        string userId,
        LLMModel model,
        LLMConversation conversation,
        Providers.LLMUsage usage,
        long responseTimeMs,
        bool hasError = false,
        string? errorMessage = null)
    {
        await _repository.TrackUsageAsync(new LLMUsage
        {
            UserId = userId,
            ModelId = model.Id,
            ConversationId = conversation.ConversationId,
            InputTokens = usage.InputTokens,
            OutputTokens = usage.OutputTokens,
            Cost = usage.Cost,
            ResponseTimeMs = (int)responseTimeMs,
            HasError = hasError,
            ErrorMessage = errorMessage,
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
