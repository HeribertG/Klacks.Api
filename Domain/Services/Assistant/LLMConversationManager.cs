// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMConversationManager
{
    private readonly ILogger<LLMConversationManager> _logger;
    private readonly ILLMRepository _repository;
    private readonly IPlaygroundConversationStore _playgroundStore;

    // Scoped per request. Both the skill-retrieval query builder and the prompt preparation
    // load the same conversation history within one turn — memoize to save a duplicate DB read.
    private readonly Dictionary<string, List<Providers.LLMMessage>> _historyCache = new();

    private const string HistoryCacheKeySeparator = "|";

    // One microsecond, the finest step the timestamptz column stores: a single tick (100 ns) is rounded away
    // there, both rows of a late persisted turn would carry the same time and the history could read the
    // answer before the request.
    private const long AssistantAfterUserTicks = TimeSpan.TicksPerMicrosecond;

    private const int MessagesPerTurn = 2;

    private const int TitleWordCount = 5;

    public LLMConversationManager(
        ILogger<LLMConversationManager> logger,
        ILLMRepository repository,
        IPlaygroundConversationStore playgroundStore)
    {
        _logger = logger;
        _repository = repository;
        _playgroundStore = playgroundStore;
    }

    private bool UsesPlaygroundStore => _playgroundStore.IsActive;

    public async Task<LLMConversation> GetOrCreateConversationAsync(string? conversationId, string userId)
    {
        var effectiveConversationId = conversationId ?? Guid.NewGuid().ToString();
        if (UsesPlaygroundStore)
        {
            return _playgroundStore.GetOrCreate(effectiveConversationId, userId);
        }

        return await _repository.GetOrCreateConversationAsync(effectiveConversationId, userId);
    }

    public async Task<List<Providers.LLMMessage>> GetConversationHistoryAsync(string conversationId, string userId)
    {
        var cacheKey = BuildHistoryCacheKey(conversationId, userId);
        if (_historyCache.TryGetValue(cacheKey, out var cached))
        {
            return new List<Providers.LLMMessage>(cached);
        }

        IEnumerable<LLMMessage> history = UsesPlaygroundStore
            ? _playgroundStore.GetMessages(conversationId, userId)
            : await _repository.GetConversationMessagesAsync(conversationId, userId);
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
        string modelId,
        DateTime? turnStartedUtc = null)
    {
        if (UsesPlaygroundStore)
        {
            SavePlaygroundTurn(conversation, userMessage, assistantMessage, modelId, turnStartedUtc);
            return;
        }

        await _repository.SaveMessageAsync(new LLMMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = userMessage,
            CreateTime = turnStartedUtc ?? DateTime.UtcNow
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
            CreateTime = turnStartedUtc?.AddTicks(AssistantAfterUserTicks) ?? DateTime.UtcNow
        });

        await _repository.RecordConversationTurnAsync(
            conversation,
            MessagesPerTurn,
            turnStartedUtc ?? DateTime.UtcNow,
            modelId,
            GenerateConversationTitle(userMessage, assistantMessage));
        _historyCache.Remove(BuildHistoryCacheKey(conversation.ConversationId, conversation.UserId));
    }

    private void SavePlaygroundTurn(
        LLMConversation conversation, string userMessage, string assistantMessage, string modelId, DateTime? turnStartedUtc)
    {
        _playgroundStore.AppendTurn(
            conversation,
            userMessage,
            ToolCallMarkupSanitizer.Sanitize(assistantMessage),
            modelId,
            turnStartedUtc ?? DateTime.UtcNow,
            GenerateConversationTitle(userMessage, assistantMessage));
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
        string? functionsCalledJson = null,
        bool toolChoiceRequested = false,
        bool toolChoiceSupported = false,
        bool toolCallReturned = false)
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
            ToolChoiceRequested = toolChoiceRequested,
            ToolChoiceSupported = toolChoiceSupported,
            ToolCallReturned = toolCallReturned,
            CreateTime = DateTime.UtcNow
        });

        if (!hasError && !UsesPlaygroundStore)
        {
            await _repository.AddConversationUsageAsync(conversation, usage.TotalTokens, usage.Cost);
        }
    }

    private string GenerateConversationTitle(string userMessage, string assistantMessage)
    {
        var words = userMessage.Split(' ').Take(TitleWordCount).ToList();
        var title = string.Join(' ', words) + (words.Count >= TitleWordCount ? "..." : "");
        return FitToTitleColumn(title);
    }

    private static string FitToTitleColumn(string title)
    {
        if (title.Length <= LLMConversation.TitleMaxLength)
        {
            return title;
        }

        var length = char.IsHighSurrogate(title[LLMConversation.TitleMaxLength - 1])
            ? LLMConversation.TitleMaxLength - 1
            : LLMConversation.TitleMaxLength;
        return title[..length];
    }
}
