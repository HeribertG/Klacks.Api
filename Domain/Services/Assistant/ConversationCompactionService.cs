// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Compresses old conversation messages into a summary via LLM call.
/// Uses the cheapest available model and stores the summary in LLMConversation.Summary.
/// </summary>
/// <param name="conversationId">Unique conversation ID for identifying the conversation</param>

using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
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
    private const int MaxSummaryTokens = 700;
    private const double SummaryTemperature = 0.3;
    private const int MaxSummaryLength = 2000;

    private static readonly string CompactionSystemPrompt =
        "You are a conversation summarizer. Read the conversation below and output a single JSON object " +
        "that captures the durable state of the conversation. The object MUST have exactly these keys: " +
        "\"openTasks\" (array of short strings: unfinished tasks or action items still pending), " +
        "\"touchedEntities\" (array of objects with \"type\", \"name\" and an optional \"id\" for domain " +
        "objects such as clients, shifts or groups that were discussed), " +
        "\"decisions\" (array of short strings: decisions the user made or confirmed), " +
        "\"facts\" (array of short strings: durable facts, user preferences and important context). " +
        "If an existing summary is provided, merge its content into the appropriate arrays instead of dropping it. " +
        "Keep every entry short and write the entry text in the same language as the conversation. " +
        "Output ONLY the raw JSON object — no markdown fences, no commentary.";

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

            var modelOutput = await GenerateSummaryAsync(conversation.Summary, oldMessages);
            if (string.IsNullOrWhiteSpace(modelOutput))
            {
                return;
            }

            conversation.Summary = BuildSummaryToStore(conversation.Summary, modelOutput);
            await _llmRepository.UpdateConversationAsync(conversation);

            _logger.LogInformation(
                "Compacted conversation {ConversationId}: summarized {MessageCount} old messages",
                conversationId.ForLog(), oldMessages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Conversation compaction failed for {ConversationId} — chat is unaffected",
                conversationId.ForLog());
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

    private static string BuildSummaryToStore(string? existingSummary, string modelOutput)
    {
        if (ConversationSummaryCodec.TryParse(modelOutput, out var structured))
        {
            MigrateLegacyFreeText(existingSummary, structured);
            return ConversationSummaryCodec.Fit(structured, MaxSummaryLength);
        }

        return Truncate(modelOutput, MaxSummaryLength);
    }

    private static void MigrateLegacyFreeText(string? existingSummary, StructuredConversationSummary target)
    {
        if (string.IsNullOrWhiteSpace(existingSummary)
            || ConversationSummaryCodec.TryParse(existingSummary, out _))
        {
            return;
        }

        var legacy = existingSummary.Trim();
        if (!target.Facts.Contains(legacy))
        {
            target.Facts.Insert(0, legacy);
        }
    }

    private static string BuildConversationText(string? existingSummary, List<DomainLLMMessage> messages)
    {
        var sb = new StringBuilder();

        var renderedExisting = ConversationSummaryCodec.RenderInner(existingSummary);
        if (!string.IsNullOrWhiteSpace(renderedExisting))
        {
            sb.AppendLine("[Previous Summary]");
            sb.AppendLine(renderedExisting);
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
