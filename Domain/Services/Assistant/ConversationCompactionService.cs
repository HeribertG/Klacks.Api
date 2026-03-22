// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Compresses old conversation messages into a summary via LLM call.
/// Uses the cheapest available model and stores the summary in LLMConversation.Summary.
/// </summary>
/// <param name="conversationId">Unique conversation ID for identifying the conversation</param>

using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

using Klacks.Api.Domain.Models.Assistant;
using DomainLLMMessage = Klacks.Api.Domain.Models.Assistant.LLMMessage;

namespace Klacks.Api.Domain.Services.Assistant;

public class ConversationCompactionService : IConversationCompactionService
{
    private readonly ILogger<ConversationCompactionService> _logger;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILLMRepository _llmRepository;

    private const int CompactionThreshold = 30;
    private const int KeepRecentMessages = 20;
    private const int MaxSummaryTokens = 300;
    private const double SummaryTemperature = 0.3;
    private const int MaxSummaryLength = 500;

    private static readonly string CompactionSystemPrompt =
        "You are a conversation summarizer. Summarize the key points of the conversation below in 2-4 sentences. " +
        "Focus on: decisions made, facts discussed, user preferences, action items, and important context. " +
        "If there is an existing summary, incorporate it. " +
        "Write the summary in the same language as the conversation. " +
        "Be concise — max 400 characters. Output ONLY the summary text, nothing else.";

    public ConversationCompactionService(
        ILogger<ConversationCompactionService> logger,
        ILLMProviderFactory providerFactory,
        ILLMRepository llmRepository)
    {
        _logger = logger;
        _providerFactory = providerFactory;
        _llmRepository = llmRepository;
    }

    public async Task CompactIfNeededAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _llmRepository.GetConversationByConversationIdAsync(conversationId);
            if (conversation == null || conversation.MessageCount < CompactionThreshold)
            {
                return;
            }

            var oldMessages = await _llmRepository.GetOldestMessagesAsync(
                conversationId, skipNewest: KeepRecentMessages, limit: 40);

            if (oldMessages.Count == 0)
            {
                return;
            }

            var summary = await GenerateSummaryAsync(conversation.Summary, oldMessages);
            if (string.IsNullOrWhiteSpace(summary))
            {
                return;
            }

            conversation.Summary = Truncate(summary, MaxSummaryLength);
            await _llmRepository.UpdateConversationAsync(conversation);

            _logger.LogInformation(
                "Compacted conversation {ConversationId}: summarized {MessageCount} old messages",
                conversationId, oldMessages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Conversation compaction failed for {ConversationId} — chat is unaffected",
                conversationId);
        }
    }

    private async Task<string?> GenerateSummaryAsync(
        string? existingSummary,
        List<DomainLLMMessage> oldMessages)
    {
        var (model, provider) = await GetCheapestModelAndProviderAsync();
        if (model == null || provider == null)
        {
            _logger.LogDebug("No enabled LLM model/provider available for conversation compaction");
            return null;
        }

        var conversationText = BuildConversationText(existingSummary, oldMessages);

        var request = new LLMProviderRequest
        {
            Message = conversationText,
            SystemPrompt = CompactionSystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = SummaryTemperature,
            MaxTokens = MaxSummaryTokens,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken
        };

        var response = await provider.ProcessAsync(request);

        if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogDebug("Compaction LLM call returned no content");
            return null;
        }

        return response.Content.Trim();
    }

    private static string BuildConversationText(string? existingSummary, List<DomainLLMMessage> messages)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(existingSummary))
        {
            sb.AppendLine("[Previous Summary]");
            sb.AppendLine(existingSummary);
            sb.AppendLine("[/Previous Summary]");
            sb.AppendLine();
        }

        sb.AppendLine("[Messages to summarize]");

        foreach (var msg in messages)
        {
            var role = msg.Role == "user" ? "User" : "Assistant";
            var content = msg.Content.Length > 300
                ? msg.Content[..300] + "..."
                : msg.Content;
            sb.AppendLine($"{role}: {content}");
        }

        sb.AppendLine("[/Messages to summarize]");

        return sb.ToString();
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

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
