// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a short, friendly status report via LLM call based on the heartbeat checklist
/// and current data points from the system. Uses the cheapest available model.
/// </summary>

using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public class HeartbeatLLMService : IHeartbeatLLMService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILLMRepository _llmRepository;
    private readonly ILogger<HeartbeatLLMService> _logger;

    private const int MaxTokens = 300;
    private const double Temperature = 0.5;

    private const string SystemPrompt =
        "You are a proactive assistant monitoring a workforce management system. " +
        "Based on the checklist and current data, create a short, friendly status report. " +
        "Only mention relevant items that actually have data. " +
        "Keep it very brief: 2-3 sentences maximum. Be direct and factual.";

    public HeartbeatLLMService(
        ILLMProviderFactory providerFactory,
        ILLMRepository llmRepository,
        ILogger<HeartbeatLLMService> logger)
    {
        _providerFactory = providerFactory;
        _llmRepository = llmRepository;
        _logger = logger;
    }

    public async Task<string?> GenerateStatusMessageAsync(
        IReadOnlyList<HeartbeatCheckItem> checkItems,
        HeartbeatDataSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (model, provider) = await GetCheapestModelAndProviderAsync();
            if (model == null || provider == null)
            {
                _logger.LogDebug("No enabled LLM model/provider available for heartbeat");
                return null;
            }

            var userMessage = BuildUserMessage(checkItems, snapshot);

            var request = new LLMProviderRequest
            {
                Message = userMessage,
                SystemPrompt = SystemPrompt,
                ModelId = model.ApiModelId,
                ConversationHistory = [],
                AvailableFunctions = [],
                Temperature = Temperature,
                MaxTokens = MaxTokens,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken
            };

            var response = await provider.ProcessAsync(request);

            if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogDebug("Heartbeat LLM call returned no content");
                return null;
            }

            return response.Content.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Heartbeat LLM call failed — notification skipped");
            return null;
        }
    }

    private static string BuildUserMessage(
        IReadOnlyList<HeartbeatCheckItem> checkItems,
        HeartbeatDataSnapshot snapshot)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Checklist items to review:");
        foreach (var item in checkItems.Where(i => i.IsEnabled))
        {
            sb.AppendLine($"- {item.Label}");
        }

        sb.AppendLine();
        sb.AppendLine($"Current system data (since {snapshot.Since:HH:mm} UTC):");
        sb.AppendLine($"- New absence requests: {snapshot.NewAbsenceRequests}");
        sb.AppendLine($"- Work entries created today: {snapshot.WorkEntriesCreatedToday}");
        sb.AppendLine($"- Schedule changes: {snapshot.NewScheduleChanges}");

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
}
